using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Shelter.Shared.IO;
using Shelter.Shared.Models;

// Shelter.Ingestion: takes a schema-conformant output.json (produced by Shelter.Research)
// and pushes each record to the LocationService API.
//
// Modes:
//   create-only  POST every record. Fails if a record already exists at same coords.
//   upsert       (DEFAULT) For each record: GET /api/locations/nearby with tiny radius;
//                if a match exists → PATCH it; otherwise POST.
//   sync         Same as upsert. Reserved for future "delete missing" behavior; not active.
//
// Usage:
//   dotnet run -- --file ../../Oblasts/Volyn/hromadas/lutsk/output.json
//   dotnet run -- --file ../../Oblasts/KyivOblast/output.json --api-url http://... --token ...
//   dotnet run -- --dir  ../../Oblasts                  (uploads every output.json found)
//   dotnet run -- --file ... --mode create-only         (legacy POST-only behavior)
//   dotnet run -- --file ... --dry-run

var argv = Environment.GetCommandLineArgs().Skip(1).ToArray();
var filePath = GetArg(argv, "--file");
var dirPath = GetArg(argv, "--dir");
var apiUrl = GetArg(argv, "--api-url") ?? PromptIfMissing("API base URL", "http://localhost:5000");
var token = GetArg(argv, "--token") ?? PromptIfMissing("API bearer token (Admin/SuperAdmin/ServiceAccount)", "", masked: true);
var dryRun = argv.Contains("--dry-run");
var mode = (GetArg(argv, "--mode") ?? "upsert").ToLowerInvariant();

if (string.IsNullOrEmpty(filePath) && string.IsNullOrEmpty(dirPath))
{
    Console.Error.WriteLine("Specify --file <path> or --dir <path>");
    return 1;
}

var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
var serializeOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
var readOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var apiBase = apiUrl.TrimEnd('/');

var files = !string.IsNullOrEmpty(filePath)
    ? new[] { Path.GetFullPath(filePath) }
    : FindOutputFiles(Path.GetFullPath(dirPath!)).ToArray();

int totalCreated = 0, totalUpdated = 0, totalUnchanged = 0, totalFail = 0;

foreach (var file in files)
{
    Console.WriteLine($"\n→ {file}  (mode={mode})");
    var doc = JsonIO.Read<OutputDocument>(file);
    Console.WriteLine($"  oblast={doc.Meta.Oblast}, hromada={doc.Meta.Hromada ?? "—"}, records={doc.Records.Count}");

    int created = 0, updated = 0, unchanged = 0, fail = 0;
    for (int i = 0; i < doc.Records.Count; i++)
    {
        var r = doc.Records[i];
        var prefix = $"  [{i + 1}/{doc.Records.Count}]";

        if (!r.Latitude.HasValue || !r.Longitude.HasValue)
        {
            Console.WriteLine($"{prefix} ⚠ skip (no coordinates): {r.Address}");
            fail++;
            continue;
        }

        try
        {
            // mode=create-only: just POST blindly (legacy behavior).
            if (mode == "create-only")
            {
                if (await PostAsync(r))
                { Console.WriteLine($"{prefix} ✓ created  {r.Address}"); created++; }
                else fail++;
                await Task.Delay(100);
                continue;
            }

            // mode=upsert: search for an existing nearby record (~10m), PATCH or POST.
            var existing = await FindNearbyMatchAsync(r);
            if (existing != null)
            {
                var diff = ComputeDiff(existing, r);
                if (diff.Count == 0)
                {
                    Console.WriteLine($"{prefix} = no change   {r.Address}");
                    unchanged++;
                }
                else
                {
                    if (await PatchAsync(existing.Id, existing.RowVersion, diff, r.Address))
                    { Console.WriteLine($"{prefix} ↺ updated   {r.Address}  (changed: {string.Join(", ", diff.Select(d => d.PropertyName))})"); updated++; }
                    else fail++;
                }
            }
            else
            {
                if (await PostAsync(r))
                { Console.WriteLine($"{prefix} ✓ created  {r.Address}"); created++; }
                else fail++;
            }
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{prefix} ✗ ERROR {r.Address}: {ex.Message}");
            fail++;
        }
    }

    Console.WriteLine($"  → {created} created, {updated} updated, {unchanged} unchanged, {fail} failed");
    totalCreated += created; totalUpdated += updated; totalUnchanged += unchanged; totalFail += fail;
}

Console.WriteLine($"\nTOTAL: {totalCreated} created, {totalUpdated} updated, {totalUnchanged} unchanged, {totalFail} failed across {files.Length} file(s).");
return totalFail == 0 ? 0 : 2;

// ─────────────────────────────────────────────────────────────────────────────

async Task<bool> PostAsync(NormalizedShelter r)
{
    if (dryRun) return true;

    // Defensive: the API has a UNIQUE(LocationId, PropertyName) constraint.
    // If output.json has duplicate PropertyNames per record (e.g. two source
    // fields both mapped to "City"), the second insert raises and the API
    // returns 500. Collapse here: keep the first non-empty value per name.
    var cmd = new
    {
        latitude = r.Latitude!.Value,
        longitude = r.Longitude!.Value,
        address = TrimLen(r.Address, 200),
        details = DedupDetails(r.Details)
    };
    var json = JsonSerializer.Serialize(cmd, serializeOpts);
    using var content = new StringContent(json, Encoding.UTF8, "application/json");
    var resp = await http.PostAsync($"{apiBase}/api/locations", content);
    if (resp.IsSuccessStatusCode) return true;
    var body = await resp.Content.ReadAsStringAsync();
    Console.WriteLine($"      POST {(int)resp.StatusCode}: {Trim(body)}");
    return false;
}

async Task<bool> PatchAsync(Guid id, uint rowVersion, List<DetailDto> changedDetails, string address)
{
    if (dryRun) return true;
    var cmd = new
    {
        rowVersion,
        details = DedupDetails(changedDetails)
    };
    var json = JsonSerializer.Serialize(cmd, serializeOpts);
    using var content = new StringContent(json, Encoding.UTF8, "application/json");
    using var req = new HttpRequestMessage(HttpMethod.Patch, $"{apiBase}/api/locations/{id}") { Content = content };
    var resp = await http.SendAsync(req);
    if (resp.IsSuccessStatusCode) return true;
    var body = await resp.Content.ReadAsStringAsync();
    Console.WriteLine($"      PATCH {(int)resp.StatusCode}: {Trim(body)}");
    return false;
}

// Returns the matching API record (within ~10m of the local record's coords with the
// same address), or null if no clear match.
async Task<ApiLocation?> FindNearbyMatchAsync(NormalizedShelter r)
{
    var url = $"{apiBase}/api/locations/nearby?latitude={r.Latitude!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}&longitude={r.Longitude!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}&radiusKm=0.05";
    var resp = await http.GetAsync(url);
    if (!resp.IsSuccessStatusCode) return null;

    var body = await resp.Content.ReadAsStringAsync();
    var candidates = JsonSerializer.Deserialize<List<ApiLocation>>(body, readOpts) ?? [];
    if (candidates.Count == 0) return null;

    // Prefer an address-equal match; fall back to the closest by simple lat/lng squared diff
    var addrMatch = candidates.FirstOrDefault(c =>
        string.Equals(c.Address?.Trim(), r.Address.Trim(), StringComparison.OrdinalIgnoreCase));
    if (addrMatch != null) return addrMatch;

    return candidates
        .OrderBy(c => Math.Pow(c.Latitude - r.Latitude.Value, 2) + Math.Pow(c.Longitude - r.Longitude.Value, 2))
        .First();
}

// Returns details that differ between local record and API record. PATCH semantics:
// empty value means "remove that property".
List<DetailDto> ComputeDiff(ApiLocation existing, NormalizedShelter local)
{
    var existingMap = (existing.Details ?? [])
        .ToDictionary(d => d.PropertyName, d => d.PropertyValue, StringComparer.OrdinalIgnoreCase);
    var localMap = local.Details
        .GroupBy(d => d.PropertyName, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(g => g.Key, g => g.Last().PropertyValue, StringComparer.OrdinalIgnoreCase);

    var diff = new List<DetailDto>();
    foreach (var (k, v) in localMap)
    {
        if (!existingMap.TryGetValue(k, out var existingVal) || existingVal != v)
            diff.Add(new DetailDto(k, v));
    }
    return diff;
}

// ─────────────────────────────────────────────────────────────────────────────

static IEnumerable<string> FindOutputFiles(string root)
{
    if (!Directory.Exists(root)) return [];
    return Directory.GetFiles(root, "output.json", SearchOption.AllDirectories);
}

static string? GetArg(string[] args, string flag)
{
    var i = Array.IndexOf(args, flag);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static string PromptIfMissing(string label, string defaultValue, bool masked = false)
{
    Console.Write($"{label}{(string.IsNullOrEmpty(defaultValue) ? "" : $" [{defaultValue}]")}: ");
    if (masked)
    {
        var sb = new StringBuilder();
        ConsoleKeyInfo k;
        while ((k = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            if (k.Key == ConsoleKey.Backspace && sb.Length > 0) sb.Length--;
            else if (!char.IsControl(k.KeyChar)) sb.Append(k.KeyChar);
        Console.WriteLine();
        return sb.Length > 0 ? sb.ToString() : defaultValue;
    }
    var line = Console.ReadLine();
    return string.IsNullOrWhiteSpace(line) ? defaultValue : line.Trim();
}

static string Trim(string s) => s.Length > 200 ? s[..200] + "…" : s;

// Server-side LocationDetail has UNIQUE(LocationId, PropertyName) and a 500-char value
// limit. Address is 200 chars. Collapse here so the API never sees a duplicate or
// over-length value.
static string TrimLen(string s, int max) =>
    string.IsNullOrEmpty(s) ? s : (s.Length > max ? s[..max] : s);

static IEnumerable<object> DedupDetails(IEnumerable<DetailDto> details) =>
    details
        .Where(d => !string.IsNullOrWhiteSpace(d.PropertyName) && !string.IsNullOrWhiteSpace(d.PropertyValue))
        .GroupBy(d => d.PropertyName.Trim(), StringComparer.OrdinalIgnoreCase)
        .Select(g => new
        {
            propertyName = TrimLen(g.Key, 50),
            propertyValue = TrimLen(g.First().PropertyValue.Trim(), 500)
        });

// Mirror of LocationDto from the API
public class ApiLocation
{
    public Guid Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
    public uint RowVersion { get; set; }
    public List<ApiLocationDetail>? Details { get; set; }
}

public class ApiLocationDetail
{
    public string PropertyName { get; set; } = "";
    public string PropertyValue { get; set; } = "";
}

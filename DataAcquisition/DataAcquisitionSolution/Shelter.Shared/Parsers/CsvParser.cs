using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Shelter.Shared.Models;

namespace Shelter.Shared.Parsers;

public class CsvParser : ISourceParser
{
    public string Format => "csv";

    public bool CanParse(string url, string contentType, byte[] firstBytes)
    {
        if (url.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) return true;
        if (url.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase)) return true;
        return contentType.Contains("csv", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<RawRecord>> ParseAsync(Stream data, string sourceUrl)
    {
        using var ms = new MemoryStream();
        await data.CopyToAsync(ms);
        ms.Position = 0;

        using var peek = new StreamReader(ms, Encoding.UTF8, leaveOpen: true);
        var firstLine = await peek.ReadLineAsync() ?? "";
        ms.Position = 0;

        var delimiter = DetectDelimiter(firstLine);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter,
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(ms, Encoding.UTF8);
        using var csv = new CsvReader(reader, config);

        var records = new List<RawRecord>();
        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];

        while (await csv.ReadAsync())
        {
            var record = new RawRecord();
            foreach (var h in headers)
            {
                var v = csv.GetField(h) ?? "";
                if (!string.IsNullOrWhiteSpace(v)) record.Fields[h] = v;
            }
            if (record.Fields.Count > 0) records.Add(record);
        }
        return records;
    }

    private static string DetectDelimiter(string line) =>
        new[] { ";", ",", "\t", "|" }
            .Select(d => (d, c: line.Count(ch => ch == d[0])))
            .OrderByDescending(x => x.c)
            .First().d;
}

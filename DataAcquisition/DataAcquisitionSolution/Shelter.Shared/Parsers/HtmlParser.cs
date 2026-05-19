using HtmlAgilityPack;
using Shelter.Shared.Models;

namespace Shelter.Shared.Parsers;

public class HtmlParser : ISourceParser
{
    public string Format => "html";

    public bool CanParse(string url, string contentType, byte[] firstBytes)
    {
        if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase)) return true;
        var head = System.Text.Encoding.UTF8.GetString(firstBytes);
        return head.Contains("<html", StringComparison.OrdinalIgnoreCase)
            || head.Contains("<!doctype", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<RawRecord>> ParseAsync(Stream data, string sourceUrl)
    {
        using var reader = new StreamReader(data);
        var html = await reader.ReadToEndAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var fromTable = ExtractFromTables(doc);
        if (fromTable.Count > 0) return fromTable;

        return ExtractFromList(doc);
    }

    private static List<RawRecord> ExtractFromTables(HtmlDocument doc)
    {
        var records = new List<RawRecord>();
        var tables = doc.DocumentNode.SelectNodes("//table");
        if (tables == null) return records;

        foreach (var table in tables)
        {
            var rows = table.SelectNodes(".//tr");
            if (rows == null || rows.Count < 2) continue;

            var headers = rows[0].SelectNodes(".//th|.//td")
                ?.Select(n => HtmlEntity.DeEntitize(n.InnerText.Trim()))
                .ToArray() ?? [];
            if (headers.Length == 0) continue;

            for (int i = 1; i < rows.Count; i++)
            {
                var cells = rows[i].SelectNodes(".//td");
                if (cells == null) continue;

                var record = new RawRecord();
                for (int j = 0; j < Math.Min(headers.Length, cells.Count); j++)
                {
                    var value = HtmlEntity.DeEntitize(cells[j].InnerText.Trim());
                    if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(headers[j]))
                        record.Fields[headers[j]] = value;
                }
                if (record.Fields.Count > 0) records.Add(record);
            }
            if (records.Count > 0) return records;
        }
        return records;
    }

    private static List<RawRecord> ExtractFromList(HtmlDocument doc)
    {
        var records = new List<RawRecord>();
        var items = doc.DocumentNode.SelectNodes("//ul/li | //ol/li");
        if (items == null) return records;

        foreach (var li in items)
        {
            var text = HtmlEntity.DeEntitize(li.InnerText.Trim());
            if (string.IsNullOrWhiteSpace(text) || text.Length < 5) continue;

            records.Add(new RawRecord
            {
                Address = text,
                Fields = new() { ["address"] = text }
            });
        }
        return records;
    }
}

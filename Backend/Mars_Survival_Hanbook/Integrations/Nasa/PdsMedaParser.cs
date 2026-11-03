using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.VisualBasic.FileIO;
using Mars_Survival_Hanbook.Services.Sources;

namespace Mars_Survival_Hanbook.Integrations.Nasa;

public record PdsSample(string ExternalObservationKey, string Quantity, decimal? OriginalValue, string? OriginalUnit,
    decimal? Value, string? Unit, string? Sclk, string? Lmst, DateTimeOffset? ObservedAt, string Quality,
    string[] Errors, string Context);
public record PdsPilotResult(string Lidvid, string Instrument, string ProcessingLevel, int TotalRows,
    bool Truncated, PdsSample[] Candidates);
public static class PdsMedaParser
{
    public static DateTimeOffset? ParseUtc(string? text) =>
        text != null && System.Text.RegularExpressions.Regex.IsMatch(text, @"(Z|[+-]\d{2}:\d{2})$") &&
        DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) && date.Year < 3000
            ? date.ToUniversalTime() : null;
    private static string? E(XContainer node, string name) => node.Descendants().FirstOrDefault(x => x.Name.LocalName == name)?.Value;
    public static PdsPilotResult Parse(XDocument label, byte[] csv, int maxSamples)
    {
        if (maxSamples is < 1 or > 1000) throw new UpstreamException("pds_sample_limit", false);
        var lid = E(label, "logical_identifier") ?? throw new UpstreamException("pds_missing_lid", false);
        var vid = E(label, "version_id") ?? throw new UpstreamException("pds_missing_version", false);
        if (!lid.StartsWith("urn:nasa:pds:mars2020_meda:data_calibrated_env:", StringComparison.Ordinal) ||
            !label.Descendants().Any(x => x.Name.LocalName == "lid_reference" && x.Value == "urn:nasa:pds:context:instrument:mars2020.meda") ||
            !label.Descendants().Any(x => x.Name.LocalName == "lid_reference" && x.Value == "urn:nasa:pds:context:target:planet.mars") ||
            E(label, "processing_level") != "Calibrated")
            throw new UpstreamException("pds_unsupported_product", false);
        var table = label.Descendants().SingleOrDefault(x => x.Name.LocalName == "Table_Delimited")
            ?? throw new UpstreamException("pds_unsupported_table", false);
        if (E(table, "field_delimiter") != "Comma" || E(table, "groups") != "0")
            throw new UpstreamException("pds_unsupported_table", false);
        var fields = table.Descendants().Where(x => x.Name.LocalName == "Field_Delimited").Select(x => new {
            Name = E(x, "name")!, Index = int.Parse(E(x, "field_number")!, CultureInfo.InvariantCulture) - 1,
            Unit = E(x, "unit"), Description = E(x, "description")?.Trim() ?? "",
            Missing = x.Descendants().Where(d => d.Parent?.Name.LocalName == "Special_Constants").Select(d => d.Value).ToArray()
        }).ToArray();
        if (!int.TryParse(E(table, "offset"), out var offset) || offset < 0 || offset > csv.Length)
            throw new UpstreamException("pds_invalid_offset", false);
        var values = fields.Where(x => x.Name is "BAROCAP1_PRESSURE" or "BAROCAP2_PRESSURE" or "BAROCAP3_PRESSURE").ToArray();
        if (values.Length == 0) throw new UpstreamException("pds_unsupported_quantity", false);
        using var input = new MemoryStream(csv, offset, csv.Length - offset);
        using var parser = new TextFieldParser(input, Encoding.UTF8) { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true };
        parser.SetDelimiters(",");
        var result = new List<PdsSample>(); var rows = 0;
        while (!parser.EndOfData)
        {
            var record = parser.ReadFields() ?? [];
            rows++;
            if (record.Length != fields.Length) throw new UpstreamException("pds_column_count", false);
            string? Field(string name) => fields.FirstOrDefault(x => x.Name == name) is { } f ? record[f.Index] : null;
            if (result.Count >= maxSamples) continue;
            foreach (var field in values)
            {
                if (result.Count >= maxSamples) break;
                var errors = new List<string>();
                var text = record[field.Index];
                decimal? value = !field.Missing.Contains(text) && decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : null;
                if (value == null) errors.Add("missing_value");
                if (field.Unit != "pascal") errors.Add("missing_or_unsupported_unit");
                var time = ParseUtc(Field("UTC"));
                if (time == null) errors.Add("utc_clock_solution_required");
                var quality = Field("QUALITY_FLAG");
                // This pilot label has no quality flag; absence must never be interpreted as good.
                if (quality is not ("0" or "VALID")) errors.Add("quality_review_required");
                var instrument = "MEDA/PS/" + field.Name;
                var key = SourceNormalization.Hash(lid + "::" + vid + "|" + instrument + "|" + Field("SCLK") + "|" + rows);
                result.Add(new(key, field.Name, value, field.Unit, field.Unit == "pascal" ? value : null,
                    field.Unit == "pascal" ? "Pa" : null, Field("SCLK"), Field("LMST"), time,
                    errors.Count == 0 ? "valid" : "pending", errors.ToArray(), field.Description));
            }
        }
        return new(lid + "::" + vid, "Mars2020/MEDA/PS", "Calibrated", rows, rows * values.Length > result.Count, result.ToArray());
    }
}

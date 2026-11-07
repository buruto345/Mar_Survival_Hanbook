using System.Text;
using System.Xml.Linq;
using Mars_Survival_Hanbook.Integrations.Nasa;
using Xunit;
namespace Mars_Survival_Hanbook.Tests;

public class PdsParserTests
{
    private static XDocument Label() => XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Nasa", "meda_pressure.xml"));
    private static byte[] Data(XDocument label, string row)
    {
        var table = label.Descendants().Single(x => x.Name.LocalName == "Table_Delimited");
        var offset = int.Parse(table.Elements().Single(x => x.Name.LocalName == "offset").Value);
        return Encoding.UTF8.GetBytes(new string(' ', offset) + row + "\r\n");
    }
    [Fact] public void OfficialPressureLabelProducesStagingWithoutInventingUtcOrGoodQuality()
    {
        var label = Label();
        var csv = Data(label, "123.001,00015M12:00:00.000,00015T12:00:00,600,1,601,1,602,1,250,1,251,1,nominal,1");
        var parsed = PdsMedaParser.Parse(label, csv, 100);
        Assert.Equal(3, parsed.Candidates.Length);
        Assert.Equal(600m, parsed.Candidates[0].OriginalValue);
        Assert.Equal("pascal", parsed.Candidates[0].OriginalUnit);
        Assert.Equal("Pa", parsed.Candidates[0].Unit);
        Assert.All(parsed.Candidates, c => {
            Assert.Null(c.ObservedAt); Assert.Equal("pending", c.Quality);
            Assert.Contains("utc_clock_solution_required", c.Errors); Assert.Contains("quality_review_required", c.Errors);
        });
        Assert.Equal(3, parsed.Candidates.Select(x => x.ExternalObservationKey).Distinct().Count());
    }
    [Fact] public void MissingValueAndUnitStayMissingRatherThanZero()
    {
        var label = Label();
        var field = label.Descendants().Single(x => x.Name.LocalName == "Field_Delimited" &&
            x.Elements().Any(e => e.Name.LocalName == "name" && e.Value == "BAROCAP1_PRESSURE"));
        field.Elements().Single(x => x.Name.LocalName == "unit").Remove();
        var result = PdsMedaParser.Parse(label, Data(label, "123,L,T,,1,601,1,602,1,250,1,251,1,nominal,1"), 1);
        var sample = Assert.Single(result.Candidates);
        Assert.Null(sample.OriginalValue); Assert.Null(sample.Value);
        Assert.Contains("missing_value", sample.Errors); Assert.Contains("missing_or_unsupported_unit", sample.Errors);
        Assert.True(result.Truncated);
        Assert.Null(PdsMedaParser.ParseUtc("3000-01-01T00:00:00Z"));
        Assert.Null(PdsMedaParser.ParseUtc("2026-01-01T00:00:00"));
    }
    [Fact] public void MalformedColumnsAndUnsupportedInstrumentFailExplicitly()
    {
        var label = Label();
        Assert.Throws<UpstreamException>(() => PdsMedaParser.Parse(label, Data(label, "1,2,3"), 10));
        label.Descendants().Single(x => x.Name.LocalName == "logical_identifier").Value = "urn:nasa:pds:other";
        Assert.Throws<UpstreamException>(() => PdsMedaParser.Parse(label, [], 10));
    }
}

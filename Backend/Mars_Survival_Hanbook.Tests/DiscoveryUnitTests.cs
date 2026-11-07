using System.Net;
using System.Text.Json;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Integrations.Nasa;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services;
using Mars_Survival_Hanbook.Services.Editorial;
using Mars_Survival_Hanbook.Services.Sources;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Mars_Survival_Hanbook.Tests;

public class DiscoveryUnitTests
{
    [Theory]
    [InlineData("https://pds.mcp.nasa.gov/api/search/1/products/test", true)]
    [InlineData("http://pds.mcp.nasa.gov/api/search/1/products/test", false)]
    [InlineData("https://pds.mcp.nasa.gov.attacker.example/test", false)]
    [InlineData("https://pds.mcp.nasa.gov:8443/test", false)]
    public void RegistryRedirectRemainsInsideExactHttpsAllowlist(string url, bool allowed)
    {
        var policy = new NasaUrlPolicy(new ConfigurationBuilder().Build(), new ProductionHost());
        if (allowed) policy.Validate(new Uri(url));
        else Assert.Throws<ApiException>(() => policy.Validate(new Uri(url)));
    }
    private sealed class ProductionHost : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public string EnvironmentName { get; set; } = "Production";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
    [Theory]
    [InlineData("http://images-assets.nasa.gov/image/PIA00407/PIA00407~orig.jpg", "https://images-assets.nasa.gov/image/PIA00407/PIA00407~orig.jpg")]
    [InlineData("http://images-assets.nasa.gov.attacker.example/a", "http://images-assets.nasa.gov.attacker.example/a")]
    [InlineData("http://images-assets.nasa.gov:8080/a", "http://images-assets.nasa.gov:8080/a")]
    [InlineData("http://127.0.0.1:4010/nasa/image~orig.png", "http://127.0.0.1:4010/nasa/image~orig.png")]
    public void LegacyImageLinksUpgradeOnlyTheOfficialDefaultPortHost(string input, string expected) =>
        Assert.Equal(expected, NasaConnectorClient.SecureImageAssetUrl(input));
    [Fact] public void UnsupportedSnapshotSchemaRequiresControlledReconstruction()
    {
        var version = new Mars_Survival_Hanbook.Models.ArticleVersion { SnapshotSchemaVersion = 2, Snapshot = "{}" };
        var failure = Assert.Throws<ApiException>(() => Mars_Survival_Hanbook.Services.Editorial.SnapshotService.Read(version));
        Assert.Equal(422, failure.Status);
        version.SnapshotSchemaVersion = 1;
        failure = Assert.Throws<ApiException>(() => Mars_Survival_Hanbook.Services.Editorial.SnapshotService.Read(version));
        Assert.Equal(422, failure.Status);
    }
    [Fact] public void CanonicalUrlPreservesContentQueryAndCase()
    {
        Assert.Equal("https://science.nasa.gov/Mars/AbC?a=1&version=2",
            SourceNormalization.CanonicalUrl("https://SCIENCE.NASA.GOV/Mars/AbC?utm_source=abc&version=2&a=1#anchor"));
        Assert.NotEqual(SourceNormalization.Identity(SourceProvider.Images, "PIA"),
            SourceNormalization.Identity(SourceProvider.Images, "pia"));
        Assert.Equal("lid:urn:nasa:pds:meda", SourceNormalization.Identity(SourceProvider.Pds, "urn:nasa:pds:meda::6.0"));
    }
    [Fact] public void FormattingAndTransportMetadataDoNotChangeCanonicalContent()
    {
        Assert.Equal(SourceNormalization.CanonicalJson("""{"b":2,"a":1,"took":2}"""),
            SourceNormalization.CanonicalJson("""{ "took":800, "a":1, "b":2 }"""));
        var a = new SourceMetadata("Mars synthetic", "https://ntrs.nasa.gov/citations/1", [], "NASA", SourceType.TechnicalReport,
            AttachmentHashes: new() { ["pdf"] = "a" });
        var b = a with { AttachmentHashes = new() { ["pdf"] = "b" } };
        Assert.NotEqual(SourceNormalization.Fingerprint(a, null), SourceNormalization.Fingerprint(b, null));
        Assert.NotEqual(SourceNormalization.Fingerprint(a, "1"), SourceNormalization.Fingerprint(a, "2"));
    }
    [Fact] public void TemperatureUsesOffsetAndPressureUsesScale()
    {
        Assert.Equal(273.15m, ContentValidation.ConvertUnit(0, "°C", "K"));
        Assert.Equal(600m, ContentValidation.ConvertUnit(6, "hPa", "Pa"));
        Assert.Equal(636m, ContentValidation.ConvertUnit(6.36m, "mb", "Pa"));
        Assert.Throws<ApiException>(() => ContentValidation.ConvertUnit(1, "K", "Pa"));
    }
    [Theory]
    [InlineData("127.0.0.1")][InlineData("10.0.0.1")][InlineData("169.254.169.254")]
    [InlineData("172.16.0.1")][InlineData("192.168.1.1")][InlineData("::1")][InlineData("::ffff:127.0.0.1")]
    [InlineData("fc00::1")][InlineData("fe80::1")]
    public void SsrfBlocksPrivateAddresses(string ip) => Assert.False(NasaUrlPolicy.IsPublic(IPAddress.Parse(ip)));
    [Fact] public void InvalidBboxAndIntegerEnumsAreRejected()
    {
        Assert.Throws<ApiException>(() => ContentValidation.Bbox("170,-10,-170,10"));
        Assert.Throws<ApiException>(() => ContentValidation.Bbox("0,0,1,100"));
        Assert.Throws<JsonException>(() => DiscoveryJson.Read<SourceDecision>("""{"decision":1,"notes":"x"}"""));
    }
    [Fact] public void RejectsNonMarsFestivalAndUnsafeXml()
    {
        Assert.False(NasaConnectorClient.MarsRelevant("Mars festival Pennsylvania"));
        Assert.True(NasaConnectorClient.MarsRelevant("Mars Global Reference Atmospheric Model"));
        Assert.Throws<System.Xml.XmlException>(() => NasaConnectorClient.ParseXml(System.Text.Encoding.UTF8.GetBytes(
            """<!DOCTYPE x [<!ENTITY e SYSTEM "file:///secret">]><x>&e;</x>""")));
    }
}

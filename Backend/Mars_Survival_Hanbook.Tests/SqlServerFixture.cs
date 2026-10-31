using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Mars_Survival_Hanbook.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (!OperatingSystem.IsWindows() || Environment.GetEnvironmentVariable("MARS_RUN_SQLSERVER_TESTS") != "1")
            Skip = "Set MARS_RUN_SQLSERVER_TESTS=1 on Windows with SQL Server LocalDB to run database tests.";
    }
}

public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string TestPrefix = "MarsHandbookModelTests_";
    private readonly string databaseName = TestPrefix + Guid.NewGuid().ToString("N");
    private bool created;
    public Guid UserId { get; private set; }
    public Guid ModuleId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid SourceId { get; private set; }
    public Guid UnitId { get; private set; }
    public Guid ArticleId { get; private set; }
    public Guid VersionId { get; private set; }

    public MarsDbContext CreateContext()
    {
        var connection = new SqlConnectionStringBuilder
        {
            DataSource = @"(localdb)\MSSQLLocalDB",
            InitialCatalog = databaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            ConnectTimeout = 30
        };
        return new MarsDbContext(new DbContextOptionsBuilder<MarsDbContext>()
            .UseSqlServer(connection.ConnectionString).Options);
    }

    public async Task InitializeAsync()
    {
        if (!OperatingSystem.IsWindows() || Environment.GetEnvironmentVariable("MARS_RUN_SQLSERVER_TESTS") != "1")
            return;

        await using var context = CreateContext();
        // The fixture only creates its own random database. It never uses app configuration.
        created = !await context.Database.CanConnectAsync();
        if (!created)
            throw new InvalidOperationException("Refusing to reuse an existing database for model tests.");

        await context.Database.MigrateAsync();

        var user = new User
        {
            Email = "reviewer@example.test", NormalizedEmail = "REVIEWER@EXAMPLE.TEST",
            DisplayName = "Test reviewer", PasswordHash = "test-placeholder-not-a-real-password",
            Role = UserRole.ScientificReviewer
        };
        var module = new Module { Name = "Test module", Slug = "test-module" };
        var category = new Category { Name = "Test category", Slug = "test-category", Module = module };
        var source = new ScientificSource
        {
            Title = "Test source metadata", NASAOrganization = "NASA",
            Url = "https://www.nasa.gov/", VerificationStatus = VerificationStatus.Pending
        };
        var unit = new Unit { Name = "Test mass rate", Symbol = "g/h", QuantityKind = "MassRate" };
        var article = new Article
        {
            Title = "Test article", Slug = "test-article", Summary = "Test summary",
            Content = "Test content", LanguageCode = "vi", Category = category, CreatedBy = user
        };
        var version = new ArticleVersion { Article = article, Version = 1, Snapshot = "{}", ChangedBy = user };
        article.Sources.Add(new ArticleSource { Source = source, ClaimNote = "Test provenance mapping" });
        context.AddRange(version, unit);
        await context.SaveChangesAsync();
        UserId = user.Id;
        ModuleId = module.Id;
        CategoryId = category.Id;
        SourceId = source.Id;
        UnitId = unit.Id;
        ArticleId = article.Id;
        VersionId = version.Id;
    }

    public async Task DisposeAsync()
    {
        if (!created)
            return;
        // Guard the destructive cleanup: only the exact random LocalDB test database is allowed.
        if (!databaseName.StartsWith(TestPrefix, StringComparison.Ordinal) ||
            !Guid.TryParseExact(databaseName[TestPrefix.Length..], "N", out _))
            throw new InvalidOperationException("Unsafe test database cleanup target.");

        await using var context = CreateContext();
        var connection = new SqlConnectionStringBuilder(context.Database.GetConnectionString());
        if (connection.DataSource != @"(localdb)\MSSQLLocalDB" || connection.InitialCatalog != databaseName)
            throw new InvalidOperationException("Test database connection changed.");
        SqlConnection.ClearAllPools();
        await context.Database.EnsureDeletedAsync();
    }

    public Article NewArticle(string? slug = null) => new()
    {
        Title = "Test article", Slug = slug ?? Guid.NewGuid().ToString("N"),
        Summary = "Test summary", Content = "Test content", LanguageCode = "vi",
        CategoryId = CategoryId, CreatedById = UserId
    };

    public ScientificFact NewFact() => new()
    {
        Name = "Test value", Value = 12.123456789012m, OriginalValue = 0.012123456789m,
        OriginalUnit = "kg/h", MeasurementContext = "Synthetic test data; no scientific claim",
        UnitId = UnitId, SourceId = SourceId
    };
}


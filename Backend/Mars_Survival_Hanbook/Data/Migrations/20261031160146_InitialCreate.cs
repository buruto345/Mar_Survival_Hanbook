using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars_Survival_Hanbook.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NasaConnectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ConnectorType = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    AuthenticationSecretReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AccessPolicy = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RateLimitPerMinute = table.Column<int>(type: "int", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NasaConnectors", x => x.Id);
                    table.CheckConstraint("CK_NasaConnectors_Configuration", "[ConfigurationJson] IS NULL OR ISJSON([ConfigurationJson]) = 1");
                    table.CheckConstraint("CK_NasaConnectors_ConnectorType", "[ConnectorType] IN (N'Images', N'Ntrs', N'Pds', N'Osdr', N'MarsTrek', N'TechPort', N'ManualArchive')");
                    table.CheckConstraint("CK_NasaConnectors_RateLimit", "[RateLimitPerMinute] IS NULL OR [RateLimitPerMinute] > 0");
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    QuantityKind = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsSi = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    State = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_Users_Role", "[Role] IN (N'RegisteredReader', N'Editor', N'ScientificReviewer', N'Administrator')");
                    table.CheckConstraint("CK_Users_State", "[State] IN (N'Active', N'Suspended', N'Deactivated')");
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.UniqueConstraint("AK_Categories_Id_ModuleId", x => new { x.Id, x.ModuleId });
                    table.CheckConstraint("CK_Categories_NotOwnParent", "[ParentId] IS NULL OR [ParentId] <> [Id]");
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentId_ModuleId",
                        columns: x => new { x.ParentId, x.ModuleId },
                        principalTable: "Categories",
                        principalColumns: new[] { "Id", "ModuleId" });
                    table.ForeignKey(
                        name: "FK_Categories_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IngestionJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RetrievedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Checksum = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ParserVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawStoragePath = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    RecordsFetched = table.Column<int>(type: "int", nullable: false),
                    RecordsAccepted = table.Column<int>(type: "int", nullable: false),
                    RecordsRejected = table.Column<int>(type: "int", nullable: false),
                    ConnectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionJobs", x => x.Id);
                    table.CheckConstraint("CK_IngestionJobs_Counts", "[AttemptCount] >= 0 AND [RecordsFetched] >= 0 AND [RecordsAccepted] >= 0 AND [RecordsRejected] >= 0 AND [RecordsAccepted] + [RecordsRejected] <= [RecordsFetched]");
                    table.CheckConstraint("CK_IngestionJobs_Status", "[Status] IN (N'Queued', N'Running', N'Succeeded', N'PartiallySucceeded', N'Failed', N'Cancelled')");
                    table.CheckConstraint("CK_IngestionJobs_TimeRange", "[CompletedAt] IS NULL OR ([StartedAt] IS NOT NULL AND [CompletedAt] >= [StartedAt])");
                    table.ForeignKey(
                        name: "FK_IngestionJobs_NasaConnectors_ConnectorId",
                        column: x => x.ConnectorId,
                        principalTable: "NasaConnectors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IngestionJobs_Users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Technologies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    EvidenceType = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    EnvironmentTested = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    Capabilities = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Limitations = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ProcessDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VerifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technologies", x => x.Id);
                    table.CheckConstraint("CK_Technologies_EnvironmentTested", "[EnvironmentTested] IN (N'Unspecified', N'Mars', N'Earth', N'InternationalSpaceStation', N'EarthAnalog', N'Simulation', N'DeepSpace')");
                    table.CheckConstraint("CK_Technologies_EvidenceType", "[EvidenceType] IN (N'Unspecified', N'Observed', N'Tested', N'Modeled', N'Proposed')");
                    table.CheckConstraint("CK_Technologies_Status", "[Status] IN (N'Proposed', N'InResearch', N'LaboratoryTested', N'Demonstrated', N'Operational', N'Retired')");
                    table.ForeignKey(
                        name: "FK_Technologies_Users_VerifiedById",
                        column: x => x.VerifiedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TranslationGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    MetaDescription = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    EstimatedReadingMinutes = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NextReviewAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PublishedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.Id);
                    table.CheckConstraint("CK_Articles_ContentType", "[ContentType] IN (N'Reference', N'Overview', N'ResearchSummary', N'TechnologyGuide')");
                    table.CheckConstraint("CK_Articles_PublicationMetadata", "[Status] <> N'Published' OR ([ReviewedAt] IS NOT NULL AND [ReviewedById] IS NOT NULL AND [PublishedAt] IS NOT NULL AND [PublishedById] IS NOT NULL)");
                    table.CheckConstraint("CK_Articles_ReadingTime", "[EstimatedReadingMinutes] IS NULL OR [EstimatedReadingMinutes] > 0");
                    table.CheckConstraint("CK_Articles_ReviewSchedule", "[NextReviewAt] IS NULL OR ([ReviewedAt] IS NOT NULL AND [NextReviewAt] >= [ReviewedAt])");
                    table.CheckConstraint("CK_Articles_Status", "[Status] IN (N'Draft', N'InReview', N'Approved', N'Published', N'Rejected', N'Archived')");
                    table.ForeignKey(
                        name: "FK_Articles_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Articles_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Articles_Users_PublishedById",
                        column: x => x.PublishedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Articles_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Articles_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ScientificSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NASAOrganization = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    CanonicalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Doi = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ExternalRecordId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Authors = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MissionOwnership = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    VerificationStatus = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    VerificationNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RetrievedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NextReviewAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Checksum = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    VerifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IngestionJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScientificSources", x => x.Id);
                    table.CheckConstraint("CK_ScientificSources_SourceType", "[SourceType] IN (N'WebPage', N'TechnicalReport', N'ResearchPaper', N'Dataset', N'Api', N'Media', N'MapLayer')");
                    table.CheckConstraint("CK_ScientificSources_Verification", "[VerificationStatus] <> N'Accepted' OR ([VerifiedAt] IS NOT NULL AND [VerifiedById] IS NOT NULL)");
                    table.CheckConstraint("CK_ScientificSources_VerificationStatus", "[VerificationStatus] IN (N'Pending', N'Accepted', N'Rejected')");
                    table.ForeignKey(
                        name: "FK_ScientificSources_IngestionJobs_IngestionJobId",
                        column: x => x.IngestionJobId,
                        principalTable: "IngestionJobs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScientificSources_Users_VerifiedById",
                        column: x => x.VerifiedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TechnologyResources",
                columns: table => new
                {
                    Direction = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    TechnologyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologyResources", x => new { x.TechnologyId, x.ResourceId, x.Direction });
                    table.CheckConstraint("CK_TechnologyResources_Direction", "[Direction] IN (N'Input', N'Output')");
                    table.ForeignKey(
                        name: "FK_TechnologyResources_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TechnologyResources_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArticleRelations",
                columns: table => new
                {
                    RelationType = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelatedArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleRelations", x => new { x.ArticleId, x.RelatedArticleId, x.RelationType });
                    table.CheckConstraint("CK_ArticleRelations_NoSelfLink", "[ArticleId] <> [RelatedArticleId]");
                    table.CheckConstraint("CK_ArticleRelations_RelationType", "[RelationType] IN (N'Related', N'Background', N'Explains', N'Mitigates', N'Uses')");
                    table.ForeignKey(
                        name: "FK_ArticleRelations_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArticleRelations_Articles_RelatedArticleId",
                        column: x => x.RelatedArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArticleResources",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleResources", x => new { x.ArticleId, x.ResourceId });
                    table.ForeignKey(
                        name: "FK_ArticleResources_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArticleResources_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArticleSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Heading = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Anchor = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleSections_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArticleTags",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleTags", x => new { x.ArticleId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ArticleTags_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArticleTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArticleTechnologies",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TechnologyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleTechnologies", x => new { x.ArticleId, x.TechnologyId });
                    table.ForeignKey(
                        name: "FK_ArticleTechnologies_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArticleTechnologies_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArticleVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Snapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangeSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleVersions", x => x.Id);
                    table.UniqueConstraint("AK_ArticleVersions_Id_ArticleId", x => new { x.Id, x.ArticleId });
                    table.CheckConstraint("CK_ArticleVersions_PositiveVersion", "[Version] > 0");
                    table.CheckConstraint("CK_ArticleVersions_ValidSnapshot", "ISJSON([Snapshot]) = 1");
                    table.ForeignKey(
                        name: "FK_ArticleVersions_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArticleVersions_Users_ChangedById",
                        column: x => x.ChangedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Bookmarks",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookmarks", x => new { x.UserId, x.ArticleId });
                    table.ForeignKey(
                        name: "FK_Bookmarks_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Bookmarks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FeedbackReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackReports", x => x.Id);
                    table.CheckConstraint("CK_FeedbackReports_Reason", "[Reason] IN (N'BrokenLink', N'ScientificAccuracy', N'OutdatedContent', N'MediaCredit', N'Accessibility', N'Other')");
                    table.CheckConstraint("CK_FeedbackReports_Resolution", "[Status] <> N'Resolved' OR ([ResolvedAt] IS NOT NULL AND [ResolvedById] IS NOT NULL)");
                    table.CheckConstraint("CK_FeedbackReports_Status", "[Status] IN (N'Open', N'InProgress', N'Resolved', N'Dismissed')");
                    table.ForeignKey(
                        name: "FK_FeedbackReports_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FeedbackReports_Users_ReportedById",
                        column: x => x.ReportedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FeedbackReports_Users_ResolvedById",
                        column: x => x.ResolvedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArticleSources",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimNote = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    EvidenceType = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    TestEnvironment = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleSources", x => new { x.ArticleId, x.SourceId });
                    table.CheckConstraint("CK_ArticleSources_EvidenceType", "[EvidenceType] IN (N'Unspecified', N'Observed', N'Tested', N'Modeled', N'Proposed')");
                    table.CheckConstraint("CK_ArticleSources_TestEnvironment", "[TestEnvironment] IN (N'Unspecified', N'Mars', N'Earth', N'InternationalSpaceStation', N'EarthAnalog', N'Simulation', N'DeepSpace')");
                    table.ForeignKey(
                        name: "FK_ArticleSources_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArticleSources_ScientificSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ScientificSources",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ChangeSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IngestionJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.CheckConstraint("CK_AuditLogs_Action", "[Action] IN (N'Created', N'Updated', N'SubmittedForReview', N'Approved', N'Rejected', N'Published', N'Unpublished', N'Archived', N'Restored', N'ImportStarted', N'ImportCompleted', N'ImportFailed', N'RoleChanged')");
                    table.CheckConstraint("CK_AuditLogs_Metadata", "[MetadataJson] IS NULL OR ISJSON([MetadataJson]) = 1");
                    table.ForeignKey(
                        name: "FK_AuditLogs_IngestionJobs_IngestionJobId",
                        column: x => x.IngestionJobId,
                        principalTable: "IngestionJobs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditLogs_ScientificSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ScientificSources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GlossaryTerms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Term = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlossaryTerms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GlossaryTerms_ScientificSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ScientificSources",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Hazards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvidenceType = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    Limitations = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hazards", x => x.Id);
                    table.CheckConstraint("CK_Hazards_EvidenceType", "[EvidenceType] IN (N'Unspecified', N'Observed', N'Tested', N'Modeled', N'Proposed')");
                    table.ForeignKey(
                        name: "FK_Hazards_ScientificSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ScientificSources",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MapLayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ServiceUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    LayerIdentifier = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TileMatrixSet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Format = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Attribution = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    VerificationStatus = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapLayers", x => x.Id);
                    table.CheckConstraint("CK_MapLayers_VerificationStatus", "[VerificationStatus] IN (N'Pending', N'Accepted', N'Rejected')");
                    table.ForeignKey(
                        name: "FK_MapLayers_ScientificSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ScientificSources",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MarsLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    CoordinateSystem = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarsLocations", x => x.Id);
                    table.CheckConstraint("CK_MarsLocations_Latitude", "[Latitude] BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_MarsLocations_Longitude", "[Longitude] BETWEEN -180 AND 180");
                    table.ForeignKey(
                        name: "FK_MarsLocations_ScientificSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ScientificSources",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NasaId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MediaType = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    OriginalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Credit = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UsageNote = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    VerificationStatus = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                    table.CheckConstraint("CK_MediaAssets_Dimensions", "([Width] IS NULL OR [Width] > 0) AND ([Height] IS NULL OR [Height] > 0)");
                    table.CheckConstraint("CK_MediaAssets_FileSize", "[FileSizeBytes] IS NULL OR [FileSizeBytes] >= 0");
                    table.CheckConstraint("CK_MediaAssets_MediaType", "[MediaType] IN (N'Image', N'Video', N'Diagram', N'Audio', N'Document')");
                    table.CheckConstraint("CK_MediaAssets_Verification", "[VerificationStatus] <> N'Accepted' OR ([VerifiedAt] IS NOT NULL AND [VerifiedById] IS NOT NULL)");
                    table.CheckConstraint("CK_MediaAssets_VerificationStatus", "[VerificationStatus] IN (N'Pending', N'Accepted', N'Rejected')");
                    table.ForeignKey(
                        name: "FK_MediaAssets_ScientificSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ScientificSources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MediaAssets_Users_VerifiedById",
                        column: x => x.VerifiedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Missions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NASAIdentifier = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Agency = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MissionType = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    LaunchDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    StatusVerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Missions", x => x.Id);
                    table.CheckConstraint("CK_Missions_DateRange", "[EndDate] IS NULL OR [LaunchDate] IS NULL OR [EndDate] >= [LaunchDate]");
                    table.CheckConstraint("CK_Missions_MissionType", "[MissionType] IN (N'Orbiter', N'Lander', N'Rover', N'Flyby', N'HumanResearch', N'TechnologyDemonstration', N'Other')");
                    table.CheckConstraint("CK_Missions_Status", "[Status] IN (N'Planned', N'Active', N'Completed', N'Cancelled', N'Unknown')");
                    table.ForeignKey(
                        name: "FK_Missions_ScientificSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ScientificSources",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResearchPapers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Abstract = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PdfUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ResearchEnvironment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SampleSize = table.Column<int>(type: "int", nullable: true),
                    Limitations = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchPapers", x => x.Id);
                    table.CheckConstraint("CK_ResearchPapers_SampleSize", "[SampleSize] IS NULL OR [SampleSize] > 0");
                    table.ForeignKey(
                        name: "FK_ResearchPapers_ScientificSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ScientificSources",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ScientificFacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    UpperValue = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: true),
                    MeasurementKind = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    OriginalValue = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    OriginalUnit = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MeasurementContext = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Assumptions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Uncertainty = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EvidenceType = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    TestEnvironment = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    VerificationStatus = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ObservedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScientificFacts", x => x.Id);
                    table.CheckConstraint("CK_ScientificFacts_EvidenceType", "[EvidenceType] IN (N'Unspecified', N'Observed', N'Tested', N'Modeled', N'Proposed')");
                    table.CheckConstraint("CK_ScientificFacts_MeasurementKind", "[MeasurementKind] IN (N'Unspecified', N'Single', N'Mean', N'Minimum', N'Maximum', N'Range', N'Estimate')");
                    table.CheckConstraint("CK_ScientificFacts_Range", "[UpperValue] IS NULL OR [UpperValue] >= [Value]");
                    table.CheckConstraint("CK_ScientificFacts_RangeKind", "[MeasurementKind] <> N'Range' OR [UpperValue] IS NOT NULL");
                    table.CheckConstraint("CK_ScientificFacts_TestEnvironment", "[TestEnvironment] IN (N'Unspecified', N'Mars', N'Earth', N'InternationalSpaceStation', N'EarthAnalog', N'Simulation', N'DeepSpace')");
                    table.CheckConstraint("CK_ScientificFacts_Verification", "[VerificationStatus] <> N'Accepted' OR ([VerifiedAt] IS NOT NULL AND [VerifiedById] IS NOT NULL AND [EvidenceType] <> N'Unspecified' AND [TestEnvironment] <> N'Unspecified')");
                    table.CheckConstraint("CK_ScientificFacts_VerificationStatus", "[VerificationStatus] IN (N'Pending', N'Accepted', N'Rejected')");
                    table.ForeignKey(
                        name: "FK_ScientificFacts_ScientificSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ScientificSources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScientificFacts_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScientificFacts_Users_VerifiedById",
                        column: x => x.VerifiedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TechnologySources",
                columns: table => new
                {
                    TechnologyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimNote = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologySources", x => new { x.TechnologyId, x.SourceId });
                    table.ForeignKey(
                        name: "FK_TechnologySources_ScientificSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ScientificSources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TechnologySources_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReviewTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    State = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourcesChecked = table.Column<bool>(type: "bit", nullable: false),
                    ClaimsChecked = table.Column<bool>(type: "bit", nullable: false),
                    UnitsChecked = table.Column<bool>(type: "bit", nullable: false),
                    EvidenceChecked = table.Column<bool>(type: "bit", nullable: false),
                    MediaRightsChecked = table.Column<bool>(type: "bit", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArticleVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewTasks", x => x.Id);
                    table.CheckConstraint("CK_ReviewTasks_Approval", "[State] <> N'Approved' OR ([ReviewerId] IS NOT NULL AND [CompletedAt] IS NOT NULL AND [SourcesChecked] = 1 AND [ClaimsChecked] = 1 AND [UnitsChecked] = 1 AND [EvidenceChecked] = 1 AND [MediaRightsChecked] = 1)");
                    table.CheckConstraint("CK_ReviewTasks_CompletionDate", "[CompletedAt] IS NULL OR [CompletedAt] >= [SubmittedAt]");
                    table.CheckConstraint("CK_ReviewTasks_State", "[State] IN (N'Pending', N'InProgress', N'Approved', N'ChangesRequested', N'Rejected', N'Cancelled')");
                    table.ForeignKey(
                        name: "FK_ReviewTasks_ArticleVersions_ArticleVersionId_ArticleId",
                        columns: x => new { x.ArticleVersionId, x.ArticleId },
                        principalTable: "ArticleVersions",
                        principalColumns: new[] { "Id", "ArticleId" });
                    table.ForeignKey(
                        name: "FK_ReviewTasks_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReviewTasks_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReviewTasks_Users_SubmittedById",
                        column: x => x.SubmittedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArticleGlossaryTerms",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GlossaryTermId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleGlossaryTerms", x => new { x.ArticleId, x.GlossaryTermId });
                    table.ForeignKey(
                        name: "FK_ArticleGlossaryTerms_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArticleGlossaryTerms_GlossaryTerms_GlossaryTermId",
                        column: x => x.GlossaryTermId,
                        principalTable: "GlossaryTerms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GlossaryAliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GlossaryTermId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlossaryAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GlossaryAliases_GlossaryTerms_GlossaryTermId",
                        column: x => x.GlossaryTermId,
                        principalTable: "GlossaryTerms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArticleHazards",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HazardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleHazards", x => new { x.ArticleId, x.HazardId });
                    table.ForeignKey(
                        name: "FK_ArticleHazards_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArticleHazards_Hazards_HazardId",
                        column: x => x.HazardId,
                        principalTable: "Hazards",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TechnologyHazards",
                columns: table => new
                {
                    TechnologyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HazardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MitigationDescription = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologyHazards", x => new { x.TechnologyId, x.HazardId });
                    table.ForeignKey(
                        name: "FK_TechnologyHazards_Hazards_HazardId",
                        column: x => x.HazardId,
                        principalTable: "Hazards",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TechnologyHazards_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArticleMedia",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MediaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleMedia", x => new { x.ArticleId, x.MediaId });
                    table.ForeignKey(
                        name: "FK_ArticleMedia_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArticleMedia_MediaAssets_MediaId",
                        column: x => x.MediaId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExplorationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    EventDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsPlanned = table.Column<bool>(type: "bit", nullable: false),
                    Agency = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExplorationEvents", x => x.Id);
                    table.CheckConstraint("CK_ExplorationEvents_DateRange", "[EndDate] IS NULL OR [EndDate] >= [EventDate]");
                    table.ForeignKey(
                        name: "FK_ExplorationEvents_Missions_MissionId",
                        column: x => x.MissionId,
                        principalTable: "Missions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExplorationEvents_ScientificSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ScientificSources",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArticleFacts",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ContextNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleFacts", x => new { x.ArticleId, x.FactId });
                    table.ForeignKey(
                        name: "FK_ArticleFacts_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArticleFacts_ScientificFacts_FactId",
                        column: x => x.FactId,
                        principalTable: "ScientificFacts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Measurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    UpperValue = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: true),
                    OriginalValue = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    OriginalUnit = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MeasurementKind = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ObservationEndAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Instrument = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Conditions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Uncertainty = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EvidenceType = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    TestEnvironment = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Measurements", x => x.Id);
                    table.CheckConstraint("CK_Measurements_EvidenceType", "[EvidenceType] IN (N'Unspecified', N'Observed', N'Tested', N'Modeled', N'Proposed')");
                    table.CheckConstraint("CK_Measurements_MeasurementKind", "[MeasurementKind] IN (N'Unspecified', N'Single', N'Mean', N'Minimum', N'Maximum', N'Range', N'Estimate')");
                    table.CheckConstraint("CK_Measurements_Range", "[UpperValue] IS NULL OR [UpperValue] >= [Value]");
                    table.CheckConstraint("CK_Measurements_RangeKind", "[MeasurementKind] <> N'Range' OR [UpperValue] IS NOT NULL");
                    table.CheckConstraint("CK_Measurements_TestEnvironment", "[TestEnvironment] IN (N'Unspecified', N'Mars', N'Earth', N'InternationalSpaceStation', N'EarthAnalog', N'Simulation', N'DeepSpace')");
                    table.CheckConstraint("CK_Measurements_TimeRange", "[ObservationEndAt] IS NULL OR [ObservationEndAt] >= [ObservedAt]");
                    table.ForeignKey(
                        name: "FK_Measurements_MarsLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "MarsLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Measurements_Missions_MissionId",
                        column: x => x.MissionId,
                        principalTable: "Missions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Measurements_ScientificFacts_FactId",
                        column: x => x.FactId,
                        principalTable: "ScientificFacts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Measurements_ScientificSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ScientificSources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Measurements_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TechnologyFacts",
                columns: table => new
                {
                    TechnologyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Criterion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ComparisonContext = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologyFacts", x => new { x.TechnologyId, x.FactId });
                    table.ForeignKey(
                        name: "FK_TechnologyFacts_ScientificFacts_FactId",
                        column: x => x.FactId,
                        principalTable: "ScientificFacts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TechnologyFacts_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleFacts_FactId",
                table: "ArticleFacts",
                column: "FactId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleGlossaryTerms_GlossaryTermId",
                table: "ArticleGlossaryTerms",
                column: "GlossaryTermId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleHazards_HazardId",
                table: "ArticleHazards",
                column: "HazardId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleMedia_ArticleId_DisplayOrder",
                table: "ArticleMedia",
                columns: new[] { "ArticleId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleMedia_MediaId",
                table: "ArticleMedia",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleRelations_RelatedArticleId",
                table: "ArticleRelations",
                column: "RelatedArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleResources_ResourceId",
                table: "ArticleResources",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ArchivedAt",
                table: "Articles",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_CategoryId_Status",
                table: "Articles",
                columns: new[] { "CategoryId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_CreatedById",
                table: "Articles",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_PublishedById",
                table: "Articles",
                column: "PublishedById");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ReviewedAt",
                table: "Articles",
                column: "ReviewedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ReviewedById",
                table: "Articles",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_Slug",
                table: "Articles",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articles_Status_NextReviewAt",
                table: "Articles",
                columns: new[] { "Status", "NextReviewAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_TranslationGroupId_LanguageCode",
                table: "Articles",
                columns: new[] { "TranslationGroupId", "LanguageCode" },
                unique: true,
                filter: "[TranslationGroupId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_UpdatedById",
                table: "Articles",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleSections_ArchivedAt",
                table: "ArticleSections",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleSections_ArticleId_Anchor",
                table: "ArticleSections",
                columns: new[] { "ArticleId", "Anchor" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleSections_ArticleId_DisplayOrder",
                table: "ArticleSections",
                columns: new[] { "ArticleId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleSources_SourceId",
                table: "ArticleSources",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleTags_TagId",
                table: "ArticleTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleTechnologies_TechnologyId",
                table: "ArticleTechnologies",
                column: "TechnologyId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleVersions_ArchivedAt",
                table: "ArticleVersions",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleVersions_ArticleId_Version",
                table: "ArticleVersions",
                columns: new[] { "ArticleId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleVersions_ChangedById",
                table: "ArticleVersions",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorId",
                table: "AuditLogs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ArchivedAt",
                table: "AuditLogs",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId_OccurredAt",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_IngestionJobId",
                table: "AuditLogs",
                column: "IngestionJobId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OccurredAt",
                table: "AuditLogs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_SourceId",
                table: "AuditLogs",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_ArticleId",
                table: "Bookmarks",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_UserId_CreatedAt",
                table: "Bookmarks",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ArchivedAt",
                table: "Categories",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ModuleId_Slug",
                table: "Categories",
                columns: new[] { "ModuleId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId_ModuleId",
                table: "Categories",
                columns: new[] { "ParentId", "ModuleId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExplorationEvents_ArchivedAt",
                table: "ExplorationEvents",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExplorationEvents_EventDate_IsPlanned",
                table: "ExplorationEvents",
                columns: new[] { "EventDate", "IsPlanned" });

            migrationBuilder.CreateIndex(
                name: "IX_ExplorationEvents_MissionId",
                table: "ExplorationEvents",
                column: "MissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExplorationEvents_SourceId",
                table: "ExplorationEvents",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackReports_ArchivedAt",
                table: "FeedbackReports",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackReports_ArticleId",
                table: "FeedbackReports",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackReports_ReportedById",
                table: "FeedbackReports",
                column: "ReportedById");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackReports_ResolvedById",
                table: "FeedbackReports",
                column: "ResolvedById");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackReports_Status_CreatedAt",
                table: "FeedbackReports",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryAliases_Alias",
                table: "GlossaryAliases",
                column: "Alias");

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryAliases_ArchivedAt",
                table: "GlossaryAliases",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryAliases_GlossaryTermId_Alias",
                table: "GlossaryAliases",
                columns: new[] { "GlossaryTermId", "Alias" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryTerms_ArchivedAt",
                table: "GlossaryTerms",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryTerms_LanguageCode_Term",
                table: "GlossaryTerms",
                columns: new[] { "LanguageCode", "Term" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryTerms_Slug",
                table: "GlossaryTerms",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryTerms_SourceId",
                table: "GlossaryTerms",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Hazards_ArchivedAt",
                table: "Hazards",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Hazards_Slug",
                table: "Hazards",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hazards_SourceId",
                table: "Hazards",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionJobs_ArchivedAt",
                table: "IngestionJobs",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionJobs_ConnectorId_Status_CreatedAt",
                table: "IngestionJobs",
                columns: new[] { "ConnectorId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IngestionJobs_RequestedById",
                table: "IngestionJobs",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_MapLayers_ArchivedAt",
                table: "MapLayers",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MapLayers_SourceId_LayerIdentifier_TileMatrixSet",
                table: "MapLayers",
                columns: new[] { "SourceId", "LayerIdentifier", "TileMatrixSet" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarsLocations_ArchivedAt",
                table: "MarsLocations",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarsLocations_Slug",
                table: "MarsLocations",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarsLocations_SourceId",
                table: "MarsLocations",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_ArchivedAt",
                table: "Measurements",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_FactId_ObservedAt",
                table: "Measurements",
                columns: new[] { "FactId", "ObservedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_LocationId",
                table: "Measurements",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_MissionId_LocationId_ObservedAt",
                table: "Measurements",
                columns: new[] { "MissionId", "LocationId", "ObservedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_SourceId",
                table: "Measurements",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_UnitId",
                table: "Measurements",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_ArchivedAt",
                table: "MediaAssets",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_MediaType_VerificationStatus",
                table: "MediaAssets",
                columns: new[] { "MediaType", "VerificationStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_NasaId",
                table: "MediaAssets",
                column: "NasaId",
                unique: true,
                filter: "[NasaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_SourceId",
                table: "MediaAssets",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_VerifiedById",
                table: "MediaAssets",
                column: "VerifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_ArchivedAt",
                table: "Missions",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_NASAIdentifier",
                table: "Missions",
                column: "NASAIdentifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Missions_SourceId",
                table: "Missions",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_Status_MissionType",
                table: "Missions",
                columns: new[] { "Status", "MissionType" });

            migrationBuilder.CreateIndex(
                name: "IX_Modules_ArchivedAt",
                table: "Modules",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_DisplayOrder",
                table: "Modules",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_Slug",
                table: "Modules",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NasaConnectors_ArchivedAt",
                table: "NasaConnectors",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NasaConnectors_Key",
                table: "NasaConnectors",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchPapers_ArchivedAt",
                table: "ResearchPapers",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchPapers_DocumentId",
                table: "ResearchPapers",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchPapers_SourceId",
                table: "ResearchPapers",
                column: "SourceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resources_ArchivedAt",
                table: "Resources",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Slug",
                table: "Resources",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewTasks_ArchivedAt",
                table: "ReviewTasks",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewTasks_ArticleId_State",
                table: "ReviewTasks",
                columns: new[] { "ArticleId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewTasks_ArticleVersionId_ArticleId",
                table: "ReviewTasks",
                columns: new[] { "ArticleVersionId", "ArticleId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewTasks_ReviewerId_State_DueAt",
                table: "ReviewTasks",
                columns: new[] { "ReviewerId", "State", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewTasks_SubmittedById",
                table: "ReviewTasks",
                column: "SubmittedById");

            migrationBuilder.CreateIndex(
                name: "IX_ScientificFacts_ArchivedAt",
                table: "ScientificFacts",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ScientificFacts_Name_EvidenceType",
                table: "ScientificFacts",
                columns: new[] { "Name", "EvidenceType" });

            migrationBuilder.CreateIndex(
                name: "IX_ScientificFacts_SourceId",
                table: "ScientificFacts",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ScientificFacts_UnitId",
                table: "ScientificFacts",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ScientificFacts_VerifiedById",
                table: "ScientificFacts",
                column: "VerifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_ScientificSources_ArchivedAt",
                table: "ScientificSources",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ScientificSources_Doi",
                table: "ScientificSources",
                column: "Doi");

            migrationBuilder.CreateIndex(
                name: "IX_ScientificSources_ExternalRecordId",
                table: "ScientificSources",
                column: "ExternalRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ScientificSources_IngestionJobId",
                table: "ScientificSources",
                column: "IngestionJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ScientificSources_VerificationStatus_NextReviewAt",
                table: "ScientificSources",
                columns: new[] { "VerificationStatus", "NextReviewAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ScientificSources_VerifiedById",
                table: "ScientificSources",
                column: "VerifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_ArchivedAt",
                table: "Tags",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Slug",
                table: "Tags",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_ArchivedAt",
                table: "Technologies",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_Slug",
                table: "Technologies",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_Status_EnvironmentTested",
                table: "Technologies",
                columns: new[] { "Status", "EnvironmentTested" });

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_VerifiedById",
                table: "Technologies",
                column: "VerifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyFacts_FactId",
                table: "TechnologyFacts",
                column: "FactId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyFacts_TechnologyId_Criterion",
                table: "TechnologyFacts",
                columns: new[] { "TechnologyId", "Criterion" });

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyHazards_HazardId",
                table: "TechnologyHazards",
                column: "HazardId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyResources_ResourceId",
                table: "TechnologyResources",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologySources_SourceId",
                table: "TechnologySources",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_ArchivedAt",
                table: "Units",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Units_Symbol_QuantityKind",
                table: "Units",
                columns: new[] { "Symbol", "QuantityKind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ArchivedAt",
                table: "Users",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleFacts");

            migrationBuilder.DropTable(
                name: "ArticleGlossaryTerms");

            migrationBuilder.DropTable(
                name: "ArticleHazards");

            migrationBuilder.DropTable(
                name: "ArticleMedia");

            migrationBuilder.DropTable(
                name: "ArticleRelations");

            migrationBuilder.DropTable(
                name: "ArticleResources");

            migrationBuilder.DropTable(
                name: "ArticleSections");

            migrationBuilder.DropTable(
                name: "ArticleSources");

            migrationBuilder.DropTable(
                name: "ArticleTags");

            migrationBuilder.DropTable(
                name: "ArticleTechnologies");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Bookmarks");

            migrationBuilder.DropTable(
                name: "ExplorationEvents");

            migrationBuilder.DropTable(
                name: "FeedbackReports");

            migrationBuilder.DropTable(
                name: "GlossaryAliases");

            migrationBuilder.DropTable(
                name: "MapLayers");

            migrationBuilder.DropTable(
                name: "Measurements");

            migrationBuilder.DropTable(
                name: "ResearchPapers");

            migrationBuilder.DropTable(
                name: "ReviewTasks");

            migrationBuilder.DropTable(
                name: "TechnologyFacts");

            migrationBuilder.DropTable(
                name: "TechnologyHazards");

            migrationBuilder.DropTable(
                name: "TechnologyResources");

            migrationBuilder.DropTable(
                name: "TechnologySources");

            migrationBuilder.DropTable(
                name: "MediaAssets");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "GlossaryTerms");

            migrationBuilder.DropTable(
                name: "MarsLocations");

            migrationBuilder.DropTable(
                name: "Missions");

            migrationBuilder.DropTable(
                name: "ArticleVersions");

            migrationBuilder.DropTable(
                name: "ScientificFacts");

            migrationBuilder.DropTable(
                name: "Hazards");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "Technologies");

            migrationBuilder.DropTable(
                name: "Articles");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "ScientificSources");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "IngestionJobs");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropTable(
                name: "NasaConnectors");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

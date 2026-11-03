using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars_Survival_Hanbook.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscoverMarsVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IngestionJobs_RequestedById",
                table: "IngestionJobs");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentAcceptedRevisionId",
                table: "ScientificSources",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentityKey",
                table: "ScientificSources",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                collation: "Latin1_General_100_BIN2");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastCheckedAt",
                table: "ScientificSources",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "ScientificSources",
                type: "nvarchar(48)",
                maxLength: 48,
                nullable: false,
                defaultValue: "ManualLegacy");

            migrationBuilder.Sql("""
                UPDATE ScientificSources
                SET IdentityKey = N'legacy:' + LOWER(CONVERT(nvarchar(36), Id))
                WHERE IdentityKey IS NULL;
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceRevisionId",
                table: "ScientificFacts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceRevisionId",
                table: "Missions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileChecksum",
                table: "MediaAssets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceRevisionId",
                table: "MediaAssets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalObservationKey",
                table: "Measurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityFlag",
                table: "Measurements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityNotes",
                table: "Measurements",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceRevisionId",
                table: "Measurements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceRevisionId",
                table: "MarsLocations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BoundingBoxJson",
                table: "MapLayers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CapabilitiesStoragePath",
                table: "MapLayers",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoordinateSystem",
                table: "MapLayers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "MapLayers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourceUrlTemplate",
                table: "MapLayers",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceRevisionId",
                table: "MapLayers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Style",
                table: "MapLayers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckpointJson",
                table: "IngestionJobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LeaseExpiresAt",
                table: "IngestionJobs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeaseOwner",
                table: "IngestionJobs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                table: "IngestionJobs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecordsFailed",
                table: "IngestionJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RecordsUnchanged",
                table: "IngestionJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RequestHash",
                table: "IngestionJobs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestJson",
                table: "IngestionJobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestKey",
                table: "IngestionJobs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceRevisionId",
                table: "ExplorationEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategorySlug",
                table: "ArticleVersions",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                table: "ArticleVersions",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReviewState",
                table: "ArticleVersions",
                type: "nvarchar(48)",
                maxLength: 48,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "ArticleVersions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedById",
                table: "ArticleVersions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchText",
                table: "ArticleVersions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SnapshotHash",
                table: "ArticleVersions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SnapshotSchemaVersion",
                table: "ArticleVersions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "ArticleVersions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ArticleVersions",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceRevisionId",
                table: "ArticleSources",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentDraftVersionId",
                table: "Articles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedVersionId",
                table: "Articles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresReview",
                table: "Articles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ArticleExplorationEvents",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExplorationEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleExplorationEvents", x => new { x.ArticleId, x.ExplorationEventId });
                    table.ForeignKey(
                        name: "FK_ArticleExplorationEvents_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArticleExplorationEvents_ExplorationEvents_ExplorationEventId",
                        column: x => x.ExplorationEventId,
                        principalTable: "ExplorationEvents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArticleLocations",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleLocations", x => new { x.ArticleId, x.LocationId });
                    table.ForeignKey(
                        name: "FK_ArticleLocations_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArticleLocations_MarsLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "MarsLocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArticleMapLayers",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MapLayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleMapLayers", x => new { x.ArticleId, x.MapLayerId });
                    table.ForeignKey(
                        name: "FK_ArticleMapLayers_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArticleMapLayers_MapLayers_MapLayerId",
                        column: x => x.MapLayerId,
                        principalTable: "MapLayers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArticleMeasurements",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeasurementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleMeasurements", x => new { x.ArticleId, x.MeasurementId });
                    table.ForeignKey(
                        name: "FK_ArticleMeasurements_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArticleMeasurements_Measurements_MeasurementId",
                        column: x => x.MeasurementId,
                        principalTable: "Measurements",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArticleMissions",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleMissions", x => new { x.ArticleId, x.MissionId });
                    table.ForeignKey(
                        name: "FK_ArticleMissions_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArticleMissions_Missions_MissionId",
                        column: x => x.MissionId,
                        principalTable: "Missions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PublicationResources",
                columns: table => new
                {
                    ArticleVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    QuantityKind = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EvidenceType = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: true),
                    MissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Date = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsPlanned = table.Column<bool>(type: "bit", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicationResources", x => new { x.ArticleVersionId, x.Kind, x.ResourceId });
                    table.CheckConstraint("CK_PublicationResources_Json", "ISJSON([Payload]) = 1");
                    table.ForeignKey(
                        name: "FK_PublicationResources_ArticleVersions_ArticleVersionId",
                        column: x => x.ArticleVersionId,
                        principalTable: "ArticleVersions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SourceRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    ProviderVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Fingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NormalizationVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MetadataSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RawStoragePath = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    RawChecksum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IngestionJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RetrievedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VerificationStatus = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    VerifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VerificationNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceRevisions", x => x.Id);
                    table.UniqueConstraint("AK_SourceRevisions_Id_SourceId", x => new { x.Id, x.SourceId });
                    table.CheckConstraint("CK_SourceRevisions_Metadata", "ISJSON([MetadataSnapshot]) = 1");
                    table.CheckConstraint("CK_SourceRevisions_Number", "[RevisionNumber] > 0");
                    table.CheckConstraint("CK_SourceRevisions_Revocation", "[RevokedAt] IS NULL OR [RevocationReason] IS NOT NULL");
                    table.CheckConstraint("CK_SourceRevisions_Verification", "[VerificationStatus] <> N'Accepted' OR ([VerifiedAt] IS NOT NULL AND [VerifiedById] IS NOT NULL)");
                    table.CheckConstraint("CK_SourceRevisions_VerificationStatus", "[VerificationStatus] IN (N'Pending', N'Accepted', N'Rejected')");
                    table.ForeignKey(
                        name: "FK_SourceRevisions_IngestionJobs_IngestionJobId",
                        column: x => x.IngestionJobId,
                        principalTable: "IngestionJobs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SourceRevisions_ScientificSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ScientificSources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SourceRevisions_Users_VerifiedById",
                        column: x => x.VerifiedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArticleVersionSources",
                columns: table => new
                {
                    ArticleVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleVersionSources", x => new { x.ArticleVersionId, x.SourceRevisionId });
                    table.ForeignKey(
                        name: "FK_ArticleVersionSources_ArticleVersions_ArticleVersionId",
                        column: x => x.ArticleVersionId,
                        principalTable: "ArticleVersions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArticleVersionSources_SourceRevisions_SourceRevisionId_SourceId",
                        columns: x => new { x.SourceRevisionId, x.SourceId },
                        principalTable: "SourceRevisions",
                        principalColumns: new[] { "Id", "SourceId" });
                });

            migrationBuilder.CreateTable(
                name: "IngestionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    IdentityKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawStoragePath = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    RawChecksum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    NormalizedPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizationVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionItems", x => x.Id);
                    table.CheckConstraint("CK_IngestionItems_Provider", "[Provider] IN (N'ManualLegacy', N'Ntrs', N'Images', N'Pds', N'NasaWeb', N'MarsTrek')");
                    table.CheckConstraint("CK_IngestionItems_Status", "[Status] IN (N'Queued', N'Processing', N'Unchanged', N'RevisionCreated', N'Rejected', N'Failed')");
                    table.ForeignKey(
                        name: "FK_IngestionItems_IngestionJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "IngestionJobs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IngestionItems_ScientificSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ScientificSources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IngestionItems_SourceRevisions_SourceRevisionId_SourceId",
                        columns: x => new { x.SourceRevisionId, x.SourceId },
                        principalTable: "SourceRevisions",
                        principalColumns: new[] { "Id", "SourceId" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScientificSources_CurrentAcceptedRevisionId_Id",
                table: "ScientificSources",
                columns: new[] { "CurrentAcceptedRevisionId", "Id" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ScientificSources_Provider",
                table: "ScientificSources",
                sql: "[Provider] IN (N'ManualLegacy', N'Ntrs', N'Images', N'Pds', N'NasaWeb', N'MarsTrek')");

            migrationBuilder.CreateIndex(
                name: "IX_ScientificFacts_SourceRevisionId_SourceId",
                table: "ScientificFacts",
                columns: new[] { "SourceRevisionId", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Missions_SourceRevisionId_SourceId",
                table: "Missions",
                columns: new[] { "SourceRevisionId", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_SourceRevisionId_SourceId",
                table: "MediaAssets",
                columns: new[] { "SourceRevisionId", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_SourceRevisionId_ExternalObservationKey",
                table: "Measurements",
                columns: new[] { "SourceRevisionId", "ExternalObservationKey" },
                unique: true,
                filter: "[SourceRevisionId] IS NOT NULL AND [ExternalObservationKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_SourceRevisionId_SourceId",
                table: "Measurements",
                columns: new[] { "SourceRevisionId", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_MarsLocations_SourceRevisionId_SourceId",
                table: "MarsLocations",
                columns: new[] { "SourceRevisionId", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_MapLayers_LocationId",
                table: "MapLayers",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_MapLayers_SourceRevisionId_SourceId",
                table: "MapLayers",
                columns: new[] { "SourceRevisionId", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_IngestionJobs_RequestedById_ConnectorId_RequestKey",
                table: "IngestionJobs",
                columns: new[] { "RequestedById", "ConnectorId", "RequestKey" },
                unique: true,
                filter: "[RequestKey] IS NOT NULL AND [RequestedById] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionJobs_Status_NextAttemptAt_LeaseExpiresAt",
                table: "IngestionJobs",
                columns: new[] { "Status", "NextAttemptAt", "LeaseExpiresAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_IngestionJobs_DetailedCounts",
                table: "IngestionJobs",
                sql: "[RecordsUnchanged] >= 0 AND [RecordsFailed] >= 0 AND [RecordsAccepted] + [RecordsRejected] + [RecordsUnchanged] + [RecordsFailed] <= [RecordsFetched]");

            migrationBuilder.CreateIndex(
                name: "IX_ExplorationEvents_SourceRevisionId_SourceId",
                table: "ExplorationEvents",
                columns: new[] { "SourceRevisionId", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleVersions_CategorySlug_LanguageCode",
                table: "ArticleVersions",
                columns: new[] { "CategorySlug", "LanguageCode" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleVersions_ReviewedById",
                table: "ArticleVersions",
                column: "ReviewedById");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ArticleVersions_ReviewState",
                table: "ArticleVersions",
                sql: "[ReviewState] IN (N'Draft', N'InReview', N'Approved', N'ChangesRequested', N'Rejected', N'Superseded')");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleSources_SourceRevisionId_SourceId",
                table: "ArticleSources",
                columns: new[] { "SourceRevisionId", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_CurrentDraftVersionId_Id",
                table: "Articles",
                columns: new[] { "CurrentDraftVersionId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_PublishedVersionId_Id",
                table: "Articles",
                columns: new[] { "PublishedVersionId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleExplorationEvents_ExplorationEventId",
                table: "ArticleExplorationEvents",
                column: "ExplorationEventId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleLocations_LocationId",
                table: "ArticleLocations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleMapLayers_MapLayerId",
                table: "ArticleMapLayers",
                column: "MapLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleMeasurements_MeasurementId",
                table: "ArticleMeasurements",
                column: "MeasurementId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleMissions_MissionId",
                table: "ArticleMissions",
                column: "MissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleVersionSources_SourceRevisionId_SourceId",
                table: "ArticleVersionSources",
                columns: new[] { "SourceRevisionId", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_IngestionItems_ArchivedAt",
                table: "IngestionItems",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionItems_JobId_ItemKey",
                table: "IngestionItems",
                columns: new[] { "JobId", "ItemKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IngestionItems_SourceId",
                table: "IngestionItems",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionItems_SourceRevisionId_SourceId",
                table: "IngestionItems",
                columns: new[] { "SourceRevisionId", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicationResources_Kind_Date",
                table: "PublicationResources",
                columns: new[] { "Kind", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicationResources_Kind_ResourceId",
                table: "PublicationResources",
                columns: new[] { "Kind", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_SourceRevisions_ArchivedAt",
                table: "SourceRevisions",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRevisions_IngestionJobId",
                table: "SourceRevisions",
                column: "IngestionJobId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRevisions_SourceId_Fingerprint",
                table: "SourceRevisions",
                columns: new[] { "SourceId", "Fingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourceRevisions_SourceId_RevisionNumber",
                table: "SourceRevisions",
                columns: new[] { "SourceId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourceRevisions_VerifiedById",
                table: "SourceRevisions",
                column: "VerifiedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_ArticleVersions_CurrentDraftVersionId_Id",
                table: "Articles",
                columns: new[] { "CurrentDraftVersionId", "Id" },
                principalTable: "ArticleVersions",
                principalColumns: new[] { "Id", "ArticleId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_ArticleVersions_PublishedVersionId_Id",
                table: "Articles",
                columns: new[] { "PublishedVersionId", "Id" },
                principalTable: "ArticleVersions",
                principalColumns: new[] { "Id", "ArticleId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleSources_SourceRevisions_SourceRevisionId_SourceId",
                table: "ArticleSources",
                columns: new[] { "SourceRevisionId", "SourceId" },
                principalTable: "SourceRevisions",
                principalColumns: new[] { "Id", "SourceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleVersions_Users_ReviewedById",
                table: "ArticleVersions",
                column: "ReviewedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExplorationEvents_SourceRevisions_SourceRevisionId_SourceId",
                table: "ExplorationEvents",
                columns: new[] { "SourceRevisionId", "SourceId" },
                principalTable: "SourceRevisions",
                principalColumns: new[] { "Id", "SourceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_MapLayers_MarsLocations_LocationId",
                table: "MapLayers",
                column: "LocationId",
                principalTable: "MarsLocations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MapLayers_SourceRevisions_SourceRevisionId_SourceId",
                table: "MapLayers",
                columns: new[] { "SourceRevisionId", "SourceId" },
                principalTable: "SourceRevisions",
                principalColumns: new[] { "Id", "SourceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_MarsLocations_SourceRevisions_SourceRevisionId_SourceId",
                table: "MarsLocations",
                columns: new[] { "SourceRevisionId", "SourceId" },
                principalTable: "SourceRevisions",
                principalColumns: new[] { "Id", "SourceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Measurements_SourceRevisions_SourceRevisionId_SourceId",
                table: "Measurements",
                columns: new[] { "SourceRevisionId", "SourceId" },
                principalTable: "SourceRevisions",
                principalColumns: new[] { "Id", "SourceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_MediaAssets_SourceRevisions_SourceRevisionId_SourceId",
                table: "MediaAssets",
                columns: new[] { "SourceRevisionId", "SourceId" },
                principalTable: "SourceRevisions",
                principalColumns: new[] { "Id", "SourceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Missions_SourceRevisions_SourceRevisionId_SourceId",
                table: "Missions",
                columns: new[] { "SourceRevisionId", "SourceId" },
                principalTable: "SourceRevisions",
                principalColumns: new[] { "Id", "SourceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ScientificFacts_SourceRevisions_SourceRevisionId_SourceId",
                table: "ScientificFacts",
                columns: new[] { "SourceRevisionId", "SourceId" },
                principalTable: "SourceRevisions",
                principalColumns: new[] { "Id", "SourceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ScientificSources_SourceRevisions_CurrentAcceptedRevisionId_Id",
                table: "ScientificSources",
                columns: new[] { "CurrentAcceptedRevisionId", "Id" },
                principalTable: "SourceRevisions",
                principalColumns: new[] { "Id", "SourceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articles_ArticleVersions_CurrentDraftVersionId_Id",
                table: "Articles");

            migrationBuilder.DropForeignKey(
                name: "FK_Articles_ArticleVersions_PublishedVersionId_Id",
                table: "Articles");

            migrationBuilder.DropForeignKey(
                name: "FK_ArticleSources_SourceRevisions_SourceRevisionId_SourceId",
                table: "ArticleSources");

            migrationBuilder.DropForeignKey(
                name: "FK_ArticleVersions_Users_ReviewedById",
                table: "ArticleVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_ExplorationEvents_SourceRevisions_SourceRevisionId_SourceId",
                table: "ExplorationEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_MapLayers_MarsLocations_LocationId",
                table: "MapLayers");

            migrationBuilder.DropForeignKey(
                name: "FK_MapLayers_SourceRevisions_SourceRevisionId_SourceId",
                table: "MapLayers");

            migrationBuilder.DropForeignKey(
                name: "FK_MarsLocations_SourceRevisions_SourceRevisionId_SourceId",
                table: "MarsLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_Measurements_SourceRevisions_SourceRevisionId_SourceId",
                table: "Measurements");

            migrationBuilder.DropForeignKey(
                name: "FK_MediaAssets_SourceRevisions_SourceRevisionId_SourceId",
                table: "MediaAssets");

            migrationBuilder.DropForeignKey(
                name: "FK_Missions_SourceRevisions_SourceRevisionId_SourceId",
                table: "Missions");

            migrationBuilder.DropForeignKey(
                name: "FK_ScientificFacts_SourceRevisions_SourceRevisionId_SourceId",
                table: "ScientificFacts");

            migrationBuilder.DropForeignKey(
                name: "FK_ScientificSources_SourceRevisions_CurrentAcceptedRevisionId_Id",
                table: "ScientificSources");

            migrationBuilder.DropTable(
                name: "ArticleExplorationEvents");

            migrationBuilder.DropTable(
                name: "ArticleLocations");

            migrationBuilder.DropTable(
                name: "ArticleMapLayers");

            migrationBuilder.DropTable(
                name: "ArticleMeasurements");

            migrationBuilder.DropTable(
                name: "ArticleMissions");

            migrationBuilder.DropTable(
                name: "ArticleVersionSources");

            migrationBuilder.DropTable(
                name: "IngestionItems");

            migrationBuilder.DropTable(
                name: "PublicationResources");

            migrationBuilder.DropTable(
                name: "SourceRevisions");

            migrationBuilder.DropIndex(
                name: "IX_ScientificSources_CurrentAcceptedRevisionId_Id",
                table: "ScientificSources");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ScientificSources_Provider",
                table: "ScientificSources");

            migrationBuilder.DropIndex(
                name: "IX_ScientificFacts_SourceRevisionId_SourceId",
                table: "ScientificFacts");

            migrationBuilder.DropIndex(
                name: "IX_Missions_SourceRevisionId_SourceId",
                table: "Missions");

            migrationBuilder.DropIndex(
                name: "IX_MediaAssets_SourceRevisionId_SourceId",
                table: "MediaAssets");

            migrationBuilder.DropIndex(
                name: "IX_Measurements_SourceRevisionId_ExternalObservationKey",
                table: "Measurements");

            migrationBuilder.DropIndex(
                name: "IX_Measurements_SourceRevisionId_SourceId",
                table: "Measurements");

            migrationBuilder.DropIndex(
                name: "IX_MarsLocations_SourceRevisionId_SourceId",
                table: "MarsLocations");

            migrationBuilder.DropIndex(
                name: "IX_MapLayers_LocationId",
                table: "MapLayers");

            migrationBuilder.DropIndex(
                name: "IX_MapLayers_SourceRevisionId_SourceId",
                table: "MapLayers");

            migrationBuilder.DropIndex(
                name: "IX_IngestionJobs_RequestedById_ConnectorId_RequestKey",
                table: "IngestionJobs");

            migrationBuilder.DropIndex(
                name: "IX_IngestionJobs_Status_NextAttemptAt_LeaseExpiresAt",
                table: "IngestionJobs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IngestionJobs_DetailedCounts",
                table: "IngestionJobs");

            migrationBuilder.DropIndex(
                name: "IX_ExplorationEvents_SourceRevisionId_SourceId",
                table: "ExplorationEvents");

            migrationBuilder.DropIndex(
                name: "IX_ArticleVersions_CategorySlug_LanguageCode",
                table: "ArticleVersions");

            migrationBuilder.DropIndex(
                name: "IX_ArticleVersions_ReviewedById",
                table: "ArticleVersions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ArticleVersions_ReviewState",
                table: "ArticleVersions");

            migrationBuilder.DropIndex(
                name: "IX_ArticleSources_SourceRevisionId_SourceId",
                table: "ArticleSources");

            migrationBuilder.DropIndex(
                name: "IX_Articles_CurrentDraftVersionId_Id",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Articles_PublishedVersionId_Id",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "CurrentAcceptedRevisionId",
                table: "ScientificSources");

            migrationBuilder.DropColumn(
                name: "IdentityKey",
                table: "ScientificSources");

            migrationBuilder.DropColumn(
                name: "LastCheckedAt",
                table: "ScientificSources");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "ScientificSources");

            migrationBuilder.DropColumn(
                name: "SourceRevisionId",
                table: "ScientificFacts");

            migrationBuilder.DropColumn(
                name: "SourceRevisionId",
                table: "Missions");

            migrationBuilder.DropColumn(
                name: "FileChecksum",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "SourceRevisionId",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "ExternalObservationKey",
                table: "Measurements");

            migrationBuilder.DropColumn(
                name: "QualityFlag",
                table: "Measurements");

            migrationBuilder.DropColumn(
                name: "QualityNotes",
                table: "Measurements");

            migrationBuilder.DropColumn(
                name: "SourceRevisionId",
                table: "Measurements");

            migrationBuilder.DropColumn(
                name: "SourceRevisionId",
                table: "MarsLocations");

            migrationBuilder.DropColumn(
                name: "BoundingBoxJson",
                table: "MapLayers");

            migrationBuilder.DropColumn(
                name: "CapabilitiesStoragePath",
                table: "MapLayers");

            migrationBuilder.DropColumn(
                name: "CoordinateSystem",
                table: "MapLayers");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "MapLayers");

            migrationBuilder.DropColumn(
                name: "ResourceUrlTemplate",
                table: "MapLayers");

            migrationBuilder.DropColumn(
                name: "SourceRevisionId",
                table: "MapLayers");

            migrationBuilder.DropColumn(
                name: "Style",
                table: "MapLayers");

            migrationBuilder.DropColumn(
                name: "CheckpointJson",
                table: "IngestionJobs");

            migrationBuilder.DropColumn(
                name: "LeaseExpiresAt",
                table: "IngestionJobs");

            migrationBuilder.DropColumn(
                name: "LeaseOwner",
                table: "IngestionJobs");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "IngestionJobs");

            migrationBuilder.DropColumn(
                name: "RecordsFailed",
                table: "IngestionJobs");

            migrationBuilder.DropColumn(
                name: "RecordsUnchanged",
                table: "IngestionJobs");

            migrationBuilder.DropColumn(
                name: "RequestHash",
                table: "IngestionJobs");

            migrationBuilder.DropColumn(
                name: "RequestJson",
                table: "IngestionJobs");

            migrationBuilder.DropColumn(
                name: "RequestKey",
                table: "IngestionJobs");

            migrationBuilder.DropColumn(
                name: "SourceRevisionId",
                table: "ExplorationEvents");

            migrationBuilder.DropColumn(
                name: "CategorySlug",
                table: "ArticleVersions");

            migrationBuilder.DropColumn(
                name: "LanguageCode",
                table: "ArticleVersions");

            migrationBuilder.DropColumn(
                name: "ReviewState",
                table: "ArticleVersions");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "ArticleVersions");

            migrationBuilder.DropColumn(
                name: "ReviewedById",
                table: "ArticleVersions");

            migrationBuilder.DropColumn(
                name: "SearchText",
                table: "ArticleVersions");

            migrationBuilder.DropColumn(
                name: "SnapshotHash",
                table: "ArticleVersions");

            migrationBuilder.DropColumn(
                name: "SnapshotSchemaVersion",
                table: "ArticleVersions");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "ArticleVersions");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "ArticleVersions");

            migrationBuilder.DropColumn(
                name: "SourceRevisionId",
                table: "ArticleSources");

            migrationBuilder.DropColumn(
                name: "CurrentDraftVersionId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "PublishedVersionId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "RequiresReview",
                table: "Articles");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionJobs_RequestedById",
                table: "IngestionJobs",
                column: "RequestedById");
        }
    }
}

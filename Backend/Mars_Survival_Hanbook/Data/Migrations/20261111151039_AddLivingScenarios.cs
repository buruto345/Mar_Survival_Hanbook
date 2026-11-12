using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars_Survival_Hanbook.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLivingScenarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LivingEnglishReviews",
                columns: table => new
                {
                    ArticleVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LivingEnglishReviews", x => x.ArticleVersionId);
                    table.ForeignKey(
                        name: "FK_LivingEnglishReviews_ArticleVersions_ArticleVersionId",
                        column: x => x.ArticleVersionId,
                        principalTable: "ArticleVersions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LivingScenarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScenarioKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Snapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LivingScenarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacilityDefinition",
                columns: table => new
                {
                    ScenarioVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TechnologyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultCount = table.Column<int>(type: "int", nullable: false),
                    MaxCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacilityDefinition", x => new { x.ScenarioVersionId, x.TechnologyId });
                    table.CheckConstraint("CK_LivingFacility_Count", "[DefaultCount]>=0 AND [MaxCount]>=[DefaultCount]");
                    table.ForeignKey(
                        name: "FK_FacilityDefinition_LivingScenarios_ScenarioVersionId",
                        column: x => x.ScenarioVersionId,
                        principalTable: "LivingScenarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FacilityDefinition_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ScenarioParameter",
                columns: table => new
                {
                    ScenarioVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    OriginalValue = table.Column<decimal>(type: "decimal(24,12)", precision: 24, scale: 12, nullable: false),
                    NormalizedValue = table.Column<decimal>(type: "decimal(24,12)", precision: 24, scale: 12, nullable: false),
                    OriginalBasis = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    NormalizedBasis = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    SourceRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioParameter", x => new { x.ScenarioVersionId, x.Key });
                    table.CheckConstraint("CK_LivingParameter_Value", "[OriginalValue]>=0 AND [NormalizedValue]>=0");
                    table.ForeignKey(
                        name: "FK_ScenarioParameter_LivingScenarios_ScenarioVersionId",
                        column: x => x.ScenarioVersionId,
                        principalTable: "LivingScenarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScenarioParameter_SourceRevisions_SourceRevisionId",
                        column: x => x.SourceRevisionId,
                        principalTable: "SourceRevisions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ScenarioResource",
                columns: table => new
                {
                    ScenarioVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InitialQuantity = table.Column<decimal>(type: "decimal(24,12)", precision: 24, scale: 12, nullable: false),
                    Capacity = table.Column<decimal>(type: "decimal(24,12)", precision: 24, scale: 12, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioResource", x => new { x.ScenarioVersionId, x.ResourceId });
                    table.CheckConstraint("CK_LivingStock_Range", "[InitialQuantity]>=0 AND [Capacity]>=[InitialQuantity]");
                    table.ForeignKey(
                        name: "FK_ScenarioResource_LivingScenarios_ScenarioVersionId",
                        column: x => x.ScenarioVersionId,
                        principalTable: "LivingScenarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScenarioResource_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScenarioResource_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FacilityFlow",
                columns: table => new
                {
                    ScenarioVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TechnologyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Phase = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    ParameterKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacilityFlow", x => new { x.ScenarioVersionId, x.TechnologyId, x.ResourceId, x.Phase, x.Direction });
                    table.CheckConstraint("CK_FacilityFlow_Direction", "[Direction] IN (N'Input', N'Output')");
                    table.ForeignKey(
                        name: "FK_FacilityFlow_FacilityDefinition_ScenarioVersionId_TechnologyId",
                        columns: x => new { x.ScenarioVersionId, x.TechnologyId },
                        principalTable: "FacilityDefinition",
                        principalColumns: new[] { "ScenarioVersionId", "TechnologyId" });
                    table.ForeignKey(
                        name: "FK_FacilityFlow_ScenarioParameter_ScenarioVersionId_ParameterKey",
                        columns: x => new { x.ScenarioVersionId, x.ParameterKey },
                        principalTable: "ScenarioParameter",
                        principalColumns: new[] { "ScenarioVersionId", "Key" });
                    table.ForeignKey(
                        name: "FK_FacilityFlow_ScenarioResource_ScenarioVersionId_ResourceId",
                        columns: x => new { x.ScenarioVersionId, x.ResourceId },
                        principalTable: "ScenarioResource",
                        principalColumns: new[] { "ScenarioVersionId", "ResourceId" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacilityDefinition_TechnologyId",
                table: "FacilityDefinition",
                column: "TechnologyId");

            migrationBuilder.CreateIndex(
                name: "IX_FacilityFlow_ScenarioVersionId_ParameterKey",
                table: "FacilityFlow",
                columns: new[] { "ScenarioVersionId", "ParameterKey" });

            migrationBuilder.CreateIndex(
                name: "IX_FacilityFlow_ScenarioVersionId_ResourceId",
                table: "FacilityFlow",
                columns: new[] { "ScenarioVersionId", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_LivingScenarios_ArchivedAt",
                table: "LivingScenarios",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LivingScenarios_ScenarioKey_Version",
                table: "LivingScenarios",
                columns: new[] { "ScenarioKey", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioParameter_SourceRevisionId",
                table: "ScenarioParameter",
                column: "SourceRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioResource_ResourceId",
                table: "ScenarioResource",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioResource_UnitId",
                table: "ScenarioResource",
                column: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacilityFlow");

            migrationBuilder.DropTable(
                name: "LivingEnglishReviews");

            migrationBuilder.DropTable(
                name: "FacilityDefinition");

            migrationBuilder.DropTable(
                name: "ScenarioParameter");

            migrationBuilder.DropTable(
                name: "ScenarioResource");

            migrationBuilder.DropTable(
                name: "LivingScenarios");
        }
    }
}

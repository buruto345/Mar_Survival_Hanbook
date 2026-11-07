using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars_Survival_Hanbook.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceDiscoverMarsIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve legacy identities; potential URL duplicates remain separate until reviewed.
            migrationBuilder.Sql("""
                UPDATE ScientificSources SET Provider = N'ManualLegacy',
                    IdentityKey = N'legacy:' + LOWER(CONVERT(nvarchar(36), Id))
                WHERE IdentityKey IS NULL OR IdentityKey = N'';
                INSERT INTO AuditLogs (Id, CreatedAt, Action, EntityType, EntityId, OccurredAt, ChangeSummary, MetadataJson)
                SELECT NEWID(), SYSDATETIMEOFFSET(), N'Unpublished', N'Article', Id, SYSDATETIMEOFFSET(),
                    N'Legacy publication requires reconstruction and scientific review; original content and versions retained.',
                    (SELECT a.Status AS previousStatus, a.PublishedAt AS publishedAt,
                        a.PublishedById AS publishedById, a.ReviewedAt AS reviewedAt,
                        a.ReviewedById AS reviewedById FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
                FROM Articles a WHERE Status = N'Published' AND PublishedVersionId IS NULL;
                UPDATE Articles SET Status = N'Draft', RequiresReview = 1
                WHERE Status = N'Published' AND PublishedVersionId IS NULL;
                """);
            migrationBuilder.AlterColumn<string>(
                name: "IdentityKey",
                table: "ScientificSources",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true,
                oldCollation: "Latin1_General_100_BIN2");

            migrationBuilder.CreateIndex(
                name: "IX_ScientificSources_Provider_IdentityKey",
                table: "ScientificSources",
                columns: new[] { "Provider", "IdentityKey" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Articles_PublicationPointer",
                table: "Articles",
                sql: "[Status] <> N'Published' OR [PublishedVersionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScientificSources_Provider_IdentityKey",
                table: "ScientificSources");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Articles_PublicationPointer",
                table: "Articles");

            migrationBuilder.AlterColumn<string>(
                name: "IdentityKey",
                table: "ScientificSources",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldCollation: "Latin1_General_100_BIN2");
        }
    }
}

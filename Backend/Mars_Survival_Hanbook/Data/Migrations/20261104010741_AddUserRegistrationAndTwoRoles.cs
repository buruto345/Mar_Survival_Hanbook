using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars_Survival_Hanbook.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRegistrationAndTwoRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Role",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "Users",
                newName: "FullName");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedUsername",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Preserve IDs, passwords, names and every existing foreign key. The GUID-based
            // username is deterministic and unique even when legacy display names collide.
            migrationBuilder.Sql("""
                UPDATE [Users]
                SET [Username] = N'legacy_' + LOWER(REPLACE(CONVERT(nvarchar(36), [Id]), N'-', N'')),
                    [NormalizedUsername] = N'LEGACY_' + UPPER(REPLACE(CONVERT(nvarchar(36), [Id]), N'-', N'')),
                    [Role] = CASE WHEN [Role] = N'RegisteredReader' THEN N'Viewer' ELSE N'Admin' END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedUsername",
                table: "Users",
                column: "NormalizedUsername",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role",
                table: "Users",
                sql: "[Role] IN (N'Viewer', N'Admin')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_NormalizedUsername",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NormalizedUsername",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "Users",
                newName: "DisplayName");

            // The former editorial subroles cannot be reconstructed after consolidation.
            migrationBuilder.Sql("UPDATE [Users] SET [Role] = CASE WHEN [Role] = N'Viewer' THEN N'RegisteredReader' ELSE N'Administrator' END;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role",
                table: "Users",
                sql: "[Role] IN (N'RegisteredReader', N'Editor', N'ScientificReviewer', N'Administrator')");
        }
    }
}

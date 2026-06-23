using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinearClone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIssuePosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Issues_TeamId_StateId_SortKey",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "SortKey",
                table: "Issues");

            migrationBuilder.AddColumn<double>(
                name: "Position",
                table: "Issues",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateIndex(
                name: "IX_Issues_TeamId_StateId_Position",
                table: "Issues",
                columns: new[] { "TeamId", "StateId", "Position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Issues_TeamId_StateId_Position",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "Issues");

            migrationBuilder.AddColumn<string>(
                name: "SortKey",
                table: "Issues",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_TeamId_StateId_SortKey",
                table: "Issues",
                columns: new[] { "TeamId", "StateId", "SortKey" });
        }
    }
}

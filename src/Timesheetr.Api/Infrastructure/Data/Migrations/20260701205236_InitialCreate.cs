using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheetr.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    TogglApiToken = table.Column<string>(type: "TEXT", nullable: false),
                    TempoApiToken = table.Column<string>(type: "TEXT", nullable: false),
                    JiraAccountId = table.Column<string>(type: "TEXT", nullable: false),
                    JiraBaseUrl = table.Column<string>(type: "TEXT", nullable: false),
                    JiraEmail = table.Column<string>(type: "TEXT", nullable: false),
                    JiraApiToken = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                    table.CheckConstraint("CK_Settings_SingleRow", "Id = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settings");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheetr.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncEntryState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncEntryStates",
                columns: table => new
                {
                    TogglId = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkspaceId = table.Column<long>(type: "INTEGER", nullable: false),
                    IssueKey = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Stage = table.Column<string>(type: "TEXT", nullable: false),
                    TempoWorklogId = table.Column<long>(type: "INTEGER", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    TogglRetryAttempt = table.Column<int>(type: "INTEGER", nullable: false),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncEntryStates", x => x.TogglId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncEntryStates");
        }
    }
}

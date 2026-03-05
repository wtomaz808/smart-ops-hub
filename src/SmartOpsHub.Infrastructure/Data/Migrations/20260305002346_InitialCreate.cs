using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartOpsHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentSessions",
                columns: table => new
                {
                    SessionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AgentType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AgentName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentSessions", x => x.SessionId);
                });

            migrationBuilder.CreateTable(
                name: "ConversationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    SessionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AgentType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MessageContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationLogs_AgentSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AgentSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentSessions_UserId",
                table: "AgentSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationLogs_SessionId",
                table: "ConversationLogs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationLogs_Timestamp",
                table: "ConversationLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationLogs_UserId",
                table: "ConversationLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationLogs");

            migrationBuilder.DropTable(
                name: "AgentSessions");
        }
    }
}

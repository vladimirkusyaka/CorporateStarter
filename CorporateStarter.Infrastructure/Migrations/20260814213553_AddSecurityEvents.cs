using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorporateStarter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SecurityEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SubjectUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubjectUserEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AuthSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RefreshTokenFamilyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DetailsJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_AuthSessionId_CreatedAtUtc",
                table: "SecurityEvents",
                columns: new[] { "AuthSessionId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_CorrelationId",
                table: "SecurityEvents",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_CreatedAtUtc",
                table: "SecurityEvents",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_EventType_CreatedAtUtc",
                table: "SecurityEvents",
                columns: new[] { "EventType", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_RefreshTokenFamilyId_CreatedAtUtc",
                table: "SecurityEvents",
                columns: new[] { "RefreshTokenFamilyId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_SubjectUserId_CreatedAtUtc",
                table: "SecurityEvents",
                columns: new[] { "SubjectUserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecurityEvents");
        }
    }
}

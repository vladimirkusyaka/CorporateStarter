using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorporateStarter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginAttemptStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoginAttemptStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginIdentifierHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FailedAttemptCount = table.Column<int>(type: "integer", nullable: false),
                    FirstFailedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastFailedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LockedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastIpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LastUserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttemptStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttemptStates_LockedUntilUtc",
                table: "LoginAttemptStates",
                column: "LockedUntilUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttemptStates_LoginIdentifierHash",
                table: "LoginAttemptStates",
                column: "LoginIdentifierHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttemptStates_UserId",
                table: "LoginAttemptStates",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginAttemptStates");
        }
    }
}

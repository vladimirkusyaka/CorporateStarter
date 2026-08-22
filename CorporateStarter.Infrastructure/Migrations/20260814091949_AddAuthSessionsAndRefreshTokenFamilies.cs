using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorporateStarter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthSessionsAndRefreshTokenFamilies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DELETE FROM "RefreshTokens";""");

            migrationBuilder.AddColumn<Guid>(
                name: "AuthSessionId",
                table: "RefreshTokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "JwtId",
                table: "RefreshTokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RefreshTokenFamilyId",
                table: "RefreshTokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "AuthSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedByIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DeviceName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CurrentJwtId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokenFamilies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ReuseDetectedByIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ReuseDetectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokenFamilies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokenFamilies_AuthSessions_AuthSessionId",
                        column: x => x.AuthSessionId,
                        principalTable: "AuthSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RefreshTokenFamilies_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_AuthSessionId",
                table: "RefreshTokens",
                column: "AuthSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_JwtId",
                table: "RefreshTokens",
                column: "JwtId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_RefreshTokenFamilyId",
                table: "RefreshTokens",
                column: "RefreshTokenFamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_CurrentJwtId",
                table: "AuthSessions",
                column: "CurrentJwtId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_UserId_RevokedAtUtc",
                table: "AuthSessions",
                columns: new[] { "UserId", "RevokedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokenFamilies_AuthSessionId_RevokedAtUtc",
                table: "RefreshTokenFamilies",
                columns: new[] { "AuthSessionId", "RevokedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokenFamilies_UserId_RevokedAtUtc",
                table: "RefreshTokenFamilies",
                columns: new[] { "UserId", "RevokedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_AuthSessions_AuthSessionId",
                table: "RefreshTokens",
                column: "AuthSessionId",
                principalTable: "AuthSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_RefreshTokenFamilies_RefreshTokenFamilyId",
                table: "RefreshTokens",
                column: "RefreshTokenFamilyId",
                principalTable: "RefreshTokenFamilies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_AuthSessions_AuthSessionId",
                table: "RefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_RefreshTokenFamilies_RefreshTokenFamilyId",
                table: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RefreshTokenFamilies");

            migrationBuilder.DropTable(
                name: "AuthSessions");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_AuthSessionId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_JwtId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_RefreshTokenFamilyId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "AuthSessionId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "JwtId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "RefreshTokenFamilyId",
                table: "RefreshTokens");
        }
    }
}

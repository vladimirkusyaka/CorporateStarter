using CorporateStarter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace CorporateStarter.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261012000000_AddProfileAppearance")]
public sealed partial class AddProfileAppearance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("PreferredTheme", "Users", type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "System");
        migrationBuilder.AddColumn<int>("PreferredPageSize", "Users", type: "integer", nullable: false, defaultValue: 20);
        migrationBuilder.AddColumn<string>("PreferredTimeZone", "Users", type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "UTC");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("PreferredTheme", "Users");
        migrationBuilder.DropColumn("PreferredPageSize", "Users");
        migrationBuilder.DropColumn("PreferredTimeZone", "Users");
    }
}

using CorporateStarter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace CorporateStarter.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261017000000_AddProfileLanguage")]
public sealed partial class AddProfileLanguage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(
        "PreferredLanguage", "Users", type: "character varying(5)", maxLength: 5, nullable: true);
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn("PreferredLanguage", "Users");
}

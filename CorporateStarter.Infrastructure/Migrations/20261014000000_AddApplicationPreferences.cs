using CorporateStarter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace CorporateStarter.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261014000000_AddApplicationPreferences")]
public sealed partial class AddApplicationPreferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("ApplicationPreferences", columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false),
            Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            Revision = table.Column<Guid>(type: "uuid", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_ApplicationPreferences", x => x.Id));
        migrationBuilder.InsertData("ApplicationPreferences", new[] { "Id", "Name", "Revision" },
            new object[] { 1, "CorporateStarter", new Guid("6d397019-ff4c-4b25-87a7-4f62b5ab28fd") });
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("ApplicationPreferences");
}

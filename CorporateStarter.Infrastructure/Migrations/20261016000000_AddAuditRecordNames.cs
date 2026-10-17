using CorporateStarter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace CorporateStarter.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261016000000_AddAuditRecordNames")]
public sealed partial class AddAuditRecordNames : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("EntityDisplayName", "AuditLogs", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>("CorrelationId", "AuditLogs", type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.CreateIndex("IX_AuditLogs_CorrelationId", "AuditLogs", "CorrelationId");
        migrationBuilder.AddColumn<string>("SubjectUserDisplayName", "SecurityEvents", type: "text", nullable: true);

        // Only historical snapshots are used. Never join today's entities to old events.
        // A temporary function safely handles malformed JSON in legacy rows.
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION pg_temp.audit_record_name(kind text, snapshot text)
            RETURNS text LANGUAGE plpgsql AS $fn$
            DECLARE doc jsonb; label text;
            BEGIN
                IF snapshot IS NULL THEN RETURN NULL; END IF;
                BEGIN doc := snapshot::jsonb;
                EXCEPTION WHEN data_exception THEN RETURN NULL;
                END;
                IF jsonb_typeof(doc) <> 'object' THEN RETURN NULL; END IF;
                IF kind = 'Person' THEN
                    label := concat_ws(' ',
                        CASE WHEN jsonb_typeof(doc->'FirstName') = 'string' THEN nullif(btrim(doc->>'FirstName'), '') END,
                        CASE WHEN jsonb_typeof(doc->'LastName') = 'string' THEN nullif(btrim(doc->>'LastName'), '') END);
                ELSIF kind = 'User' THEN
                    label := coalesce(
                        CASE WHEN jsonb_typeof(doc->'DisplayName') = 'string' THEN nullif(btrim(doc->>'DisplayName'), '') END,
                        CASE WHEN jsonb_typeof(doc->'Login') = 'string' THEN nullif(btrim(doc->>'Login'), '') END);
                ELSIF kind IN ('Country', 'City', 'Company', 'Position', 'Role', 'Permission', 'ApplicationPreferences') THEN
                    IF jsonb_typeof(doc->'Name') = 'string' THEN label := btrim(doc->>'Name'); END IF;
                END IF;
                RETURN nullif(label, '');
            END;
            $fn$;
            UPDATE "AuditLogs"
            SET "EntityDisplayName" = coalesce(
                pg_temp.audit_record_name("EntityName", "NewValuesJson"),
                pg_temp.audit_record_name("EntityName", "OldValuesJson"))
            WHERE "EntityDisplayName" IS NULL
              AND "EntityName" IN ('Person', 'User', 'Country', 'City', 'Company', 'Position', 'Role', 'Permission', 'ApplicationPreferences');
            DROP FUNCTION pg_temp.audit_record_name(text, text);
            """);
        // Old security events keep their stored email/ID fallback; correlation cannot
        // be reconstructed reliably for old Changes, so it stays NULL.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_AuditLogs_CorrelationId", "AuditLogs");
        migrationBuilder.DropColumn("CorrelationId", "AuditLogs");
        migrationBuilder.DropColumn("EntityDisplayName", "AuditLogs");
        migrationBuilder.DropColumn("SubjectUserDisplayName", "SecurityEvents");
    }
}

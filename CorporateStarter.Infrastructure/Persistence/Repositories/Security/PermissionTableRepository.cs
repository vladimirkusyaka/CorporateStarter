using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Infrastructure.Persistence.Tables;
using CorporateStarter.Shared.Dtos.Permissions;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.Security;

// The existing Permissions.Read policy includes the entire system catalog.
public sealed class PermissionTableRepository(AppDbContext db)
    : SqlTableRepository<PermissionListItemDto>(db), IPermissionTableRepository
{
    protected override string Table => "\"Permissions\"";
    protected override string Projection => "\"Id\", \"Code\", \"Name\", \"Description\", \"Group\", \"IsActive\"";
    protected override string DefaultOrder => "lower(\"Group\") asc, lower(\"Code\") asc";
    protected override IReadOnlyDictionary<string, SqlTableColumn> Columns { get; } =
        new Dictionary<string, SqlTableColumn>(StringComparer.OrdinalIgnoreCase)
        {
            ["code"] = new("\"Code\""),
            ["name"] = new("\"Name\""),
            ["description"] = new("\"Description\""),
            ["group"] = new("\"Group\""),
            ["isActive"] = new("\"IsActive\"", true)
        };
}

using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Infrastructure.Persistence.Tables;
using CorporateStarter.Shared.Dtos.Security.Roles;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.Security;

public sealed class RoleTableRepository(AppDbContext db, IEntityAccess access)
    : ActiveSqlTableRepository<RoleListItemDto>(db, access, "roles"), IRoleTableRepository
{
    protected override string Table => "\"Roles\"";
    protected override string Projection => "\"Id\", \"Name\", \"Description\", \"IsSystemRole\", \"IsActive\"";
    protected override IReadOnlyDictionary<string, SqlTableColumn> Columns { get; } =
        new Dictionary<string, SqlTableColumn>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = new("\"Name\""),
            ["type"] = new("CASE WHEN \"IsSystemRole\" THEN 'System' ELSE 'Custom' END"),
            ["description"] = new("\"Description\""),
            ["isActive"] = new("\"IsActive\"", true)
        };
}

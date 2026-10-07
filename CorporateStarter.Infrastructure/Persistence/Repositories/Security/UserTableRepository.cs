using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Infrastructure.Persistence.Tables;
using CorporateStarter.Shared.Dtos.Security.Users;
namespace CorporateStarter.Infrastructure.Persistence.Repositories.Security;

public sealed class UserTableRepository(AppDbContext db)
    : SqlTableRepository<UserTableItemDto>(db), IUserTableRepository
{
    protected override string Table => "\"Users\"";
    protected override string Projection => "\"Id\", \"Login\", \"Email\", \"DisplayName\", \"IsActive\"";
    protected override string DefaultOrder => "lower(\"Login\") asc";
    protected override IReadOnlyDictionary<string, SqlTableColumn> Columns { get; } =
        new Dictionary<string, SqlTableColumn>(StringComparer.OrdinalIgnoreCase)
        {
            ["login"] = new("\"Login\""),
            ["email"] = new("\"Email\""),
            ["displayName"] = new("\"DisplayName\""),
            ["isActive"] = new("\"IsActive\"", true)
        };
}

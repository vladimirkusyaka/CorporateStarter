using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Positions;
using CorporateStarter.Infrastructure.Persistence.Tables;
using CorporateStarter.Shared.Dtos.MasterData.Positions;
namespace CorporateStarter.Infrastructure.Persistence.Repositories.MasterData.Positions;

// Positions.Read already grants access to active and inactive positions in the existing CRUD API.
public sealed class PositionTableRepository(AppDbContext db)
    : SqlTableRepository<PositionListItemDto>(db), IPositionTableRepository
{
    protected override string Table => "\"Positions\"";
    protected override string Projection => "\"Id\", \"Name\", \"Description\", \"IsActive\"";
    protected override IReadOnlyDictionary<string, SqlTableColumn> Columns { get; } =
        new Dictionary<string, SqlTableColumn>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = new("\"Name\""),
            ["description"] = new("\"Description\""),
            ["isActive"] = new("\"IsActive\"", true)
        };
}

using CorporateStarter.Application.Common.Interfaces.Repositories.Audit;
using CorporateStarter.Infrastructure.Persistence.Tables;
using CorporateStarter.Shared.Dtos.Audit;
using Microsoft.EntityFrameworkCore;
namespace CorporateStarter.Infrastructure.Persistence.Repositories.Audit;

public sealed class ChangeTableRepository : SqlTableRepository<ChangeListItemDto>, IChangeTableRepository
{
    private readonly AppDbContext _db;
    public ChangeTableRepository(AppDbContext db) : base(db) => _db = db;
    // Keep all historical records. Only this view excludes authentication internals.
    private static readonly string[] TechnicalEntities = ["SecurityEvent", "AuthSession", "RefreshToken", "RefreshTokenFamily", "LoginAttemptState", "PasswordHistory"];
    protected override string Table => "\"AuditLogs\"";
    protected override string Projection => """
        "Id", "EntityName", "EntityId", "EntityDisplayName", "CorrelationId", "Action", "UserEmail", "CreatedAtUtc"
        """;
    protected override string SecurityPredicate => "\"EntityName\" NOT IN (" + string.Join(",", TechnicalEntities.Select(x => "'" + x + "'")) + ")";
    protected override string DefaultOrder => "\"CreatedAtUtc\" desc";
    protected override IReadOnlyDictionary<string, SqlTableColumn> Columns { get; } = new Dictionary<string, SqlTableColumn>(StringComparer.OrdinalIgnoreCase)
    {
        ["createdAtUtc"] = new(SqlDateText.Utc("\"CreatedAtUtc\"")),
        ["entityName"] = new("\"EntityName\""),
        ["entityId"] = new("\"EntityId\""),
        ["id"] = new("\"Id\"::text"),
        ["entityDisplayName"] = new("coalesce(\"EntityDisplayName\", \"EntityId\")"),
        ["correlationId"] = new("\"CorrelationId\""),
        ["action"] = new("\"Action\""),
        ["userEmail"] = new("\"UserEmail\"")
    };
    public Task<AuditLogListItemDto?> GetDetailsAsync(Guid id, CancellationToken ct) => _db.AuditLogs.AsNoTracking()
        .Where(x => x.Id == id && !TechnicalEntities.Contains(x.EntityName))
        .Select(x => new AuditLogListItemDto
        {
            Id = x.Id,
            EntityName = x.EntityName,
            EntityId = x.EntityId,
            EntityDisplayName = x.EntityDisplayName,
            CorrelationId = x.CorrelationId,
            Action = x.Action,
            UserEmail = x.UserEmail,
            CreatedAtUtc = x.CreatedAtUtc,
            OldValuesJson = x.OldValuesJson,
            NewValuesJson = x.NewValuesJson
        })
        .SingleOrDefaultAsync(ct);
}

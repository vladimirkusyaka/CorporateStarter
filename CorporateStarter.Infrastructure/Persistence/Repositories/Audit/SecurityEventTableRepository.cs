using CorporateStarter.Application.Common.Interfaces.Repositories.Audit;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Infrastructure.Persistence.Tables;
using CorporateStarter.Shared.Dtos.Audit;
using Microsoft.EntityFrameworkCore;
namespace CorporateStarter.Infrastructure.Persistence.Repositories.Audit;

public sealed class SecurityEventTableRepository : SqlTableRepository<SecurityEventRowDto>, ISecurityEventTableRepository
{
    private readonly AppDbContext _db;
    public SecurityEventTableRepository(AppDbContext db) : base(db) => _db = db;
    protected override string Table => "\"SecurityEvents\"";
    protected override string Projection => """
        "Id", "CreatedAtUtc", "EventType", "Severity", "Outcome", "SubjectUserEmail", "SubjectUserDisplayName", "SubjectUserId", "IpAddress", "CorrelationId"
        """;
    protected override string DefaultOrder => "\"CreatedAtUtc\" desc";
    protected override IReadOnlyDictionary<string, SqlTableColumn> Columns { get; } = new Dictionary<string, SqlTableColumn>(StringComparer.OrdinalIgnoreCase)
    {
        ["createdAtUtc"] = new(SqlDateText.Utc("\"CreatedAtUtc\"")),
        ["eventType"] = new("\"EventType\""),
        ["severity"] = new("\"Severity\""),
        ["outcome"] = new("\"Outcome\""),
        ["id"] = new("\"Id\"::text"),
        ["subjectUserId"] = new("\"SubjectUserId\"::text"),
        ["subjectUserDisplayName"] = new("coalesce(\"SubjectUserDisplayName\", \"SubjectUserEmail\", \"SubjectUserId\"::text)"),
        ["subjectUserEmail"] = new("\"SubjectUserEmail\""),
        ["ipAddress"] = new("\"IpAddress\""),
        ["correlationId"] = new("\"CorrelationId\"")
    };
    public Task<SecurityEventListItemDto?> GetDetailsAsync(Guid id, CancellationToken ct) => _db.SecurityEvents.AsNoTracking()
        .Where(x => x.Id == id)
        .Select(x => new SecurityEventListItemDto
        {
            Id = x.Id,
            CreatedAtUtc = x.CreatedAtUtc,
            EventType = x.EventType,
            Severity = x.Severity,
            Outcome = x.Outcome,
            SubjectUserId = x.SubjectUserId,
            SubjectUserEmail = x.SubjectUserEmail,
            SubjectUserDisplayName = x.SubjectUserDisplayName,
            AuthSessionId = x.AuthSessionId,
            RefreshTokenFamilyId = x.RefreshTokenFamilyId,
            CorrelationId = x.CorrelationId,
            IpAddress = x.IpAddress,
            UserAgent = x.UserAgent,
            DetailsJson = x.DetailsJson
        }).SingleOrDefaultAsync(ct);
}

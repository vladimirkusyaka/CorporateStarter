using MediatR;
using CorporateStarter.Shared.Dtos.Audit;

namespace CorporateStarter.Application.Audit.Queries
{
    public sealed record GetAuditLogsQuery
    : IRequest<IReadOnlyList<AuditLogListItemDto>>;
}

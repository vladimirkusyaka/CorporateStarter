using MediatR;
using CorporateStarter.Shared.Dtos.Audit;

namespace CorporateStarter.Application.Audit.Changes.Queries
{
    public sealed record GetAuditLogsQuery
    : IRequest<IReadOnlyList<AuditLogListItemDto>>;
}

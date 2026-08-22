using MediatR;
using CorporateStarter.Shared.Dtos.Audit;
using CorporateStarter.Application.Common.Interfaces.Repositories.Audit;
using CorporateStarter.Application.Audit.Queries;

namespace CorporateStarter.Application.Audit.Handlers
{
    public sealed class GetAuditsQueryHandler
        : IRequestHandler<GetAuditLogsQuery, IReadOnlyList<AuditLogListItemDto>>
    {
        private readonly IAuditReadRepository _repository;

        public GetAuditsQueryHandler(IAuditReadRepository repository)
        {
            _repository = repository;
        }
        public async Task<IReadOnlyList<AuditLogListItemDto>> Handle(
                GetAuditLogsQuery request,
                CancellationToken cancellationToken)
        {
            return await _repository.GetListAsync(cancellationToken);
        }
    }
}
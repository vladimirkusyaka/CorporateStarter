using CorporateStarter.Application.Audit.SecurityEvents.Queries;
using CorporateStarter.Application.Common.Interfaces.Repositories.Audit;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Audit;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Audit.SecurityEvents.Handlers
{
    public sealed class SearchSecurityEventsQueryHandler(
    ISecurityEventRepository repository)
    : IRequestHandler<SearchSecurityEventsQuery, PagedResult<SecurityEventListItemDto>>
    {
        public Task<PagedResult<SecurityEventListItemDto>> Handle(
            SearchSecurityEventsQuery request,
            CancellationToken cancellationToken)
        {
            return repository.SearchAsync(request.Request, cancellationToken);
        }
    }
}

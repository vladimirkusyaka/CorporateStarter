using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Security.SecurityEvents.Queries;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Security.SecurityEvents;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Security.SecurityEvents.Handlers
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

using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Audit;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Audit.SecurityEvents.Queries
{
    public sealed record SearchSecurityEventsQuery(SecurityEventSearchRequest Request)
    : IRequest<PagedResult<SecurityEventListItemDto>>;
}

using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Security.SecurityEvents;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Security.SecurityEvents.Queries
{
    public sealed record SearchSecurityEventsQuery(SecurityEventSearchRequest Request)
    : IRequest<PagedResult<SecurityEventListItemDto>>;
}

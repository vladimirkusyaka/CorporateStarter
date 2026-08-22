using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.Security.SecurityEvents.Queries;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Security.SecurityEvents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CorporateStarter.Api.Controllers.Security
{
    [ApiController]
    [Route("api/security-events")]
    [Authorize]
    public sealed class SecurityEventsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SecurityEventsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = AppPermissions.AuditRead)]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<SecurityEventListItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<SecurityEventListItemDto>>> Search(
            [FromQuery] SecurityEventSearchRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new SearchSecurityEventsQuery(request),
                cancellationToken);

            return Ok(result);
        }
    }
}

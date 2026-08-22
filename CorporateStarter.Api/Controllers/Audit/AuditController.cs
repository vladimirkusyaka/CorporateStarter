using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using CorporateStarter.Application.Audit.Queries;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Dtos.Audit;

namespace CorporateStarter.Api.Controllers.Audit
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuditController(IMediator mediator) : Controller
    {
        private readonly IMediator _mediator = mediator;

        [Authorize(Policy = AppPermissions.AuditRead)]
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AuditLogListItemDto>>> GetAll(
                CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetAuditLogsQuery(),
                cancellationToken);

            return Ok(result);
        }
    }
}
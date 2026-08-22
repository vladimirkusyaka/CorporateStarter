using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.Security.Permissions.Queries;
using CorporateStarter.Shared.Dtos.Permissions;

namespace CorporateStarter.Api.Controllers.Security
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public sealed class PermissionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PermissionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = AppPermissions.PermissionsRead)]
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<PermissionListItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<PermissionListItemDto>>> GetAll(
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetPermissionsQuery(),
                cancellationToken);

            return Ok(result);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.Security.Roles.Commands;
using CorporateStarter.Application.Security.Roles.Queries;
using CorporateStarter.Shared.Dtos.Security.Roles;


namespace CorporateStarter.Api.Controllers.Security
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class RolesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RolesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = AppPermissions.RolesRead)]
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<RoleListItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<RoleListItemDto>>> GetAll(
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetRolesQuery(),
                cancellationToken);

            return Ok(result);
        }

        [Authorize(Policy = AppPermissions.RolesRead)]
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(RoleDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RoleDetailsDto>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetRoleByIdQuery(id),
                cancellationToken);

            return result is null
                ? NotFound()
                : Ok(result);
        }

        [Authorize(Policy = AppPermissions.RolesCreate)]
        [HttpPost]
        [ProducesResponseType(typeof(CommandResult<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommandResult<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CommandResult<Guid>>> Create(
            CreateRoleRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new CreateRoleCommand(request),
                cancellationToken);

            return result.Succeeded
                ? Ok(result)
                : BadRequest(result);
        }

        [Authorize(Policy = AppPermissions.RolesUpdate)]
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(CommandResult<Unit>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommandResult<Unit>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CommandResult<Unit>>> Update(
            Guid id,
            UpdateRoleRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new UpdateRoleCommand(id, request),
                cancellationToken);

            return result.Succeeded
                ? Ok(result)
                : BadRequest(result);
        }

        [Authorize(Policy = AppPermissions.RolesDelete)]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(CommandResult<Unit>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommandResult<Unit>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CommandResult<Unit>>> Delete(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new DeleteRoleCommand(id),
                cancellationToken);

            return result.Succeeded
                ? Ok(result)
                : BadRequest(result);
        }
    }
}

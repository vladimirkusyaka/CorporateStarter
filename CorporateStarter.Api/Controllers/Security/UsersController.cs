using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.Security.Users.Commands;
using CorporateStarter.Application.Security.Users.Queries;
using CorporateStarter.Shared.Dtos.Security.Users;


namespace CorporateStarter.Api.Controllers.Security
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = AppPermissions.UsersRead)]
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<UserListItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> GetAll(
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetUsersQuery(),
                cancellationToken);

            return Ok(result);
        }

        [Authorize(Policy = AppPermissions.UsersRead)]
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDetailsDto>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetUserByIdQuery(id),
                cancellationToken);

            return result is null
                ? NotFound()
                : Ok(result);
        }

        [Authorize(Policy = AppPermissions.UsersCreate)]
        [HttpPost]
        [ProducesResponseType(typeof(CommandResult<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommandResult<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CommandResult<Guid>>> Create(
            CreateUserRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new CreateUserCommand(request),
                cancellationToken);

            return result.Succeeded
                ? Ok(result)
                : BadRequest(result);
        }

        [Authorize(Policy = AppPermissions.UsersUpdate)]
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(CommandResult<Unit>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommandResult<Unit>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CommandResult<Unit>>> Update(
            Guid id,
            UpdateUserRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new UpdateUserCommand(id, request),
                cancellationToken);

            return result.Succeeded
                ? Ok(result)
                : BadRequest(result);
        }

        [Authorize(Policy = AppPermissions.UsersChangePassword)]
        [HttpPut("{id:guid}/password")]
        [ProducesResponseType(typeof(CommandResult<Unit>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommandResult<Unit>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CommandResult<Unit>>> ChangePassword(
            Guid id,
            ChangeUserPasswordRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new ChangeUserPasswordCommand(id, request),
                cancellationToken);

            return result.Succeeded
                ? Ok(result)
                : BadRequest(result);
        }

        [Authorize(Policy = AppPermissions.UsersDelete)]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(CommandResult<Unit>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommandResult<Unit>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CommandResult<Unit>>> Delete(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new DeleteUserCommand(id),
                cancellationToken);

            return result.Succeeded
                ? Ok(result)
                : BadRequest(result);
        }
    }
}

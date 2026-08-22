using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.Directory.Persons.Commands;
using CorporateStarter.Application.Directory.Persons.Queries;
using CorporateStarter.Shared.Dtos.Directory.Persons;


namespace CorporateStarter.Api.Controllers.Directory
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public sealed class PersonsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PersonsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = AppPermissions.PersonsRead)]
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<PersonListItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<PersonListItemDto>>> GetAll(
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetPersonsQuery(),
                cancellationToken);

            return Ok(result);
        }

        [Authorize(Policy = AppPermissions.PersonsRead)]
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(PersonDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PersonDetailsDto>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetPersonByIdQuery(id),
                cancellationToken);

            if (result is null)
                return NotFound();

            return Ok(result);
        }

        [Authorize(Policy = AppPermissions.PersonsCreate)]
        [HttpPost]
        [ProducesResponseType(typeof(PersonDetailsDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PersonDetailsDto>> Create(
            [FromBody] CreatePersonRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new CreatePersonCommand(request),
                cancellationToken);

            if (!result.Succeeded)
                return ToErrorResult(result);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Value!.Id },
                result.Value);
        }

        [Authorize(Policy = AppPermissions.PersonsUpdate)]
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(PersonDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PersonDetailsDto>> Update(
            Guid id,
            [FromBody] UpdatePersonRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new UpdatePersonCommand(id, request),
                cancellationToken);

            if (!result.Succeeded)
                return ToErrorResult(result);

            return Ok(result.Value);
        }

        [Authorize(Policy = AppPermissions.PersonsDelete)]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new DeletePersonCommand(id),
                cancellationToken);

            if (!result.Succeeded)
                return ToErrorResult(result);

            return NoContent();
        }

        private ActionResult ToErrorResult<T>(CommandResult<T> result)
        {
            var statusCode = result.ErrorCode switch
            {
                "person.not_found" => StatusCodes.Status404NotFound,
                "person.company_not_found" => StatusCodes.Status404NotFound,
                "person.position_not_found" => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status400BadRequest
            };

            return Problem(
                title: result.ErrorMessage,
                statusCode: statusCode,
                extensions: new Dictionary<string, object?>
                {
                    ["errorCode"] = result.ErrorCode
                });
        }
    }
}
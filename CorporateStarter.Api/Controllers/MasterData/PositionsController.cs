using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.MasterData.Positions.Commands;
using CorporateStarter.Application.MasterData.Positions.Queries;
using CorporateStarter.Shared.Dtos.MasterData.Positions;


namespace CorporateStarter.Api.Controllers.MasterData;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class PositionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PositionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.PositionsRead)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PositionListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PositionListItemDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetPositionsQuery(),
            cancellationToken);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PositionsRead)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PositionDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PositionDetailsDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetPositionByIdQuery(id),
            cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PositionsCreate)]
    [HttpPost]
    [ProducesResponseType(typeof(PositionDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PositionDetailsDto>> Create(
        [FromBody] CreatePositionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreatePositionCommand(request),
            cancellationToken);

        if (!result.Succeeded)
            return ToErrorResult(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            result.Value);
    }

    [Authorize(Policy = AppPermissions.PositionsUpdate)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PositionDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PositionDetailsDto>> Update(
        Guid id,
        [FromBody] UpdatePositionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdatePositionCommand(id, request),
            cancellationToken);

        if (!result.Succeeded)
            return ToErrorResult(result);

        return Ok(result.Value);
    }

    [Authorize(Policy = AppPermissions.PositionsDelete)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeletePositionCommand(id),
            cancellationToken);

        if (!result.Succeeded)
            return ToErrorResult(result);

        return NoContent();
    }

    private ActionResult ToErrorResult<T>(CommandResult<T> result)
    {
        var statusCode = result.ErrorCode switch
        {
            "position.not_found" => StatusCodes.Status404NotFound,
            "position.name_exists" => StatusCodes.Status409Conflict,
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

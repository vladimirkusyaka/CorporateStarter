using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.MasterData.Cities.Commands;
using CorporateStarter.Application.MasterData.Cities.Queries;
using CorporateStarter.Shared.Dtos.MasterData.Cities;


namespace CorporateStarter.Api.Controllers.MasterData;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class CitiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CitiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.CitiesRead)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CityListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CityListItemDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetCitiesQuery(),
            cancellationToken);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.CitiesRead)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CityDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CityDetailsDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetCityByIdQuery(id),
            cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.CitiesCreate)]
    [HttpPost]
    [ProducesResponseType(typeof(CityDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CityDetailsDto>> Create(
        [FromBody] CreateCityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateCityCommand(request),
            cancellationToken);

        if (!result.Succeeded)
            return ToErrorResult(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            result.Value);
    }

    [Authorize(Policy = AppPermissions.CitiesUpdate)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CityDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CityDetailsDto>> Update(
        Guid id,
        [FromBody] UpdateCityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateCityCommand(id, request),
            cancellationToken);

        if (!result.Succeeded)
            return ToErrorResult(result);

        return Ok(result.Value);
    }

    [Authorize(Policy = AppPermissions.CitiesDelete)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeleteCityCommand(id),
            cancellationToken);

        if (!result.Succeeded)
            return ToErrorResult(result);

        return NoContent();
    }

    private ActionResult ToErrorResult<T>(CommandResult<T> result)
    {
        var statusCode = result.ErrorCode switch
        {
            "city.not_found" => StatusCodes.Status404NotFound,
            "city.country_not_found" => StatusCodes.Status404NotFound,
            "city.name_exists" => StatusCodes.Status409Conflict,
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.MasterData.Countries.Commands;
using CorporateStarter.Application.MasterData.Countries.Queries;
using CorporateStarter.Shared.Dtos.MasterData.Countries;


namespace CorporateStarter.Api.Controllers.MasterData;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class CountriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CountriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.CountriesRead)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CountryListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CountryListItemDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetCountriesQuery(),
            cancellationToken);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.CountriesRead)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CountryDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CountryDetailsDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetCountryByIdQuery(id),
            cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.CountriesCreate)]
    [HttpPost]
    [ProducesResponseType(typeof(CountryDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CountryDetailsDto>> Create(
        [FromBody] CreateCountryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateCountryCommand(request),
            cancellationToken);

        if (!result.Succeeded)
            return ToErrorResult(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            result.Value);
    }

    [Authorize(Policy = AppPermissions.CountriesUpdate)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CountryDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CountryDetailsDto>> Update(
        Guid id,
        [FromBody] UpdateCountryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateCountryCommand(id, request),
            cancellationToken);

        if (!result.Succeeded)
            return ToErrorResult(result);

        return Ok(result.Value);
    }

    [Authorize(Policy = AppPermissions.CountriesDelete)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeleteCountryCommand(id),
            cancellationToken);

        if (!result.Succeeded)
            return ToErrorResult(result);

        return NoContent();
    }

    private ActionResult ToErrorResult<T>(CommandResult<T> result)
    {
        var statusCode = result.ErrorCode switch
        {
            "country.not_found" => StatusCodes.Status404NotFound,
            "country.code_exists" => StatusCodes.Status409Conflict,
            "country.name_exists" => StatusCodes.Status409Conflict,
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

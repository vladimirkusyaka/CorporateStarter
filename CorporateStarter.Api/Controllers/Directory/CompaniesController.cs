using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.Directory.Companies.Commands;
using CorporateStarter.Application.Directory.Companies.Queries;
using CorporateStarter.Shared.Dtos.Directory.Companies;


namespace CorporateStarter.Api.Controllers.Directory;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class CompaniesController : ControllerBase
{
    private readonly ISender _sender;

    public CompaniesController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize(Policy = AppPermissions.CompaniesRead)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CompanyListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CompanyListItemDto>>> GetList(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetCompaniesQuery(),
            cancellationToken);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.CompaniesRead)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CompanyDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanyDetailsDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetCompanyByIdQuery(id),
            cancellationToken);

        return result is null
            ? NotFound()
            : Ok(result);
    }

    [Authorize(Policy = AppPermissions.CompaniesCreate)]
    [HttpPost]
    [ProducesResponseType(typeof(CompanyDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CompanyDetailsDto>> Create(
        CreateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateCompanyCommand(request),
            cancellationToken);

        if (!result.Succeeded)
            return ToErrorResult(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            result.Value);
    }

    [Authorize(Policy = AppPermissions.CompaniesUpdate)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CompanyDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanyDetailsDto>> Update(
        Guid id,
        UpdateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateCompanyCommand(id, request),
            cancellationToken);

        if (!result.Succeeded)
            return ToErrorResult(result);

        return Ok(result.Value);
    }

    [Authorize(Policy = AppPermissions.CompaniesDelete)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteCompanyCommand(id),
            cancellationToken);

        if (!result.Succeeded)
            return ToErrorResult(result);

        return NoContent();
    }

    private ActionResult ToErrorResult<T>(CommandResult<T> result)
    {
        var statusCode = result.ErrorCode switch
        {
            "company.not_found" => StatusCodes.Status404NotFound,
            "company.city_not_found" => StatusCodes.Status404NotFound,
            "company.code_exists" => StatusCodes.Status409Conflict,
            "company.name_exists" => StatusCodes.Status409Conflict,
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
using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Shared.Dtos.Directory.Companies;

namespace CorporateStarter.Application.Directory.Companies.Commands
{
    public sealed record UpdateCompanyCommand(
        Guid Id,
        UpdateCompanyRequest Request)
        : IRequest<CommandResult<CompanyDetailsDto>>;
}

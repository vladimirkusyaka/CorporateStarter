using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Shared.Dtos.Directory.Companies;

namespace CorporateStarter.Application.Directory.Companies.Queries
{
    public sealed record GetCompanyByIdQuery(Guid Id)
        : IRequest<CompanyDetailsDto?>;
}

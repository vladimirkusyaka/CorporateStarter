using MediatR;
using CorporateStarter.Shared.Dtos.Directory.Companies;

namespace CorporateStarter.Application.Directory.Companies.Queries
{
    public sealed record GetCompaniesQuery
    : IRequest<IReadOnlyList<CompanyListItemDto>>;
}

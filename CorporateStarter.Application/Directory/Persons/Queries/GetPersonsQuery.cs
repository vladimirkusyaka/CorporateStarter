using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Shared.Dtos.Directory.Persons;

namespace CorporateStarter.Application.Directory.Persons.Queries
{
    public sealed record GetPersonsQuery
    : IRequest<IReadOnlyList<PersonListItemDto>>;
}

using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Security.Users.Queries;
using CorporateStarter.Shared.Dtos.Security.Users;

namespace CorporateStarter.Application.Security.Users.Handlers
{
    public sealed class GetUserByIdQueryHandler(IUserReadRepository repository)
        : IRequestHandler<GetUserByIdQuery, UserDetailsDto?>
    {
        private readonly IUserReadRepository _repository = repository;

        public Task<UserDetailsDto?> Handle(
            GetUserByIdQuery request,
            CancellationToken cancellationToken)
        {
            return _repository.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}

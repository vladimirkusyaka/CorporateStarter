using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Persons;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.Directory.Persons.Commands;

namespace CorporateStarter.Application.Directory.Persons.Handlers
{
    public sealed class DeletePersonCommandHandler
            : IRequestHandler<DeletePersonCommand, CommandResult<Unit>>
    {
        private readonly IPersonWriteRepository _personWriteRepository;

        public DeletePersonCommandHandler(IPersonWriteRepository personWriteRepository)
        {
            _personWriteRepository = personWriteRepository;
        }

        public async Task<CommandResult<Unit>> Handle(
            DeletePersonCommand request,
            CancellationToken cancellationToken)
        {
            var deactivated = await _personWriteRepository.DeactivateAsync(
                request.Id,
                cancellationToken);

            if (!deactivated)
            {
                return CommandResult<Unit>.Failure(
                    "person.not_found",
                    "Person was not found.");
            }

            await _personWriteRepository.SaveChangesAsync(cancellationToken);

            return CommandResult<Unit>.Success(Unit.Value);
        }
    }
}

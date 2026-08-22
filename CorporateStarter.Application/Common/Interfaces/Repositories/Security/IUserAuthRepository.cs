using CorporateStarter.Application.Common.Security;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.Security
{
    public interface IUserAuthRepository
    {
        Task<UserAuthInfo?> GetByLoginOrEmailAsync(
            string loginOrEmail,
            CancellationToken cancellationToken);

        Task<UserAuthInfo?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken);
    }
}

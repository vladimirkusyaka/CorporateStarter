using CorporateStarter.Shared.Dtos.Auth;
using CorporateStarter.Application.Common.Security;

namespace CorporateStarter.Application.Common.Interfaces.Security
{
    public interface IJwtTokenService
    {
        JwtTokenResult CreateToken(UserProfileDto user);

        JwtTokenResult CreateToken(
            UserProfileDto user,
            Guid authSessionId);
    }
}

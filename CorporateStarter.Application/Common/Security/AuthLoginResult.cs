using CorporateStarter.Shared.Dtos.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Security
{
    public sealed class AuthLoginResult
    {
        public AuthSessionResult? Session { get; set; }

        public MfaChallengeResponse? MfaChallenge { get; set; }

        public bool RequiresMfa => MfaChallenge is not null;

        public static AuthLoginResult Authenticated(AuthSessionResult session)
        {
            return new AuthLoginResult
            {
                Session = session
            };
        }

        public static AuthLoginResult MfaRequired(MfaChallengeResponse challenge)
        {
            return new AuthLoginResult
            {
                MfaChallenge = challenge
            };
        }
    }
}

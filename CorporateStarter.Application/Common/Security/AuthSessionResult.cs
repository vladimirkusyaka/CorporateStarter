using System;
using System.Text;
using System.Collections.Generic;
using CorporateStarter.Shared.Dtos.Auth;

namespace CorporateStarter.Application.Common.Security
{
    public sealed class AuthSessionResult
    {
        public LoginResponse Response { get; set; } = null!;

        public string RefreshToken { get; set; } = string.Empty;

        public DateTime RefreshTokenExpiresAtUtc { get; set; }
    }
}

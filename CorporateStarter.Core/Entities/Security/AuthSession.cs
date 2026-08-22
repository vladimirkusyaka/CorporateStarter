using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Core.Entities.Security
{
    public sealed class AuthSession
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime LastSeenAtUtc { get; set; }

        public DateTime? RevokedAtUtc { get; set; }

        public string? RevokedByIp { get; set; }

        public string? CreatedByIp { get; set; }

        public string? UserAgent { get; set; }

        public string? DeviceName { get; set; }

        public string? CurrentJwtId { get; set; }

        public bool IsRevoked => RevokedAtUtc.HasValue;

        public User User { get; set; } = null!;

        public ICollection<RefreshTokenFamily> RefreshTokenFamilies { get; set; } = [];

        public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    }
}

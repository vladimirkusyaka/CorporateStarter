using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Core.Entities.Security
{
    public sealed class RefreshToken
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime? RevokedAtUtc { get; set; }

        public string? ReplacedByTokenHash { get; set; }

        public string? CreatedByIp { get; set; }

        public string? RevokedByIp { get; set; }

        public string? UserAgent { get; set; }

        public bool IsRevoked => RevokedAtUtc.HasValue;

        public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

        public bool IsActive => !IsRevoked && !IsExpired;

        public Guid AuthSessionId { get; set; }

        public Guid RefreshTokenFamilyId { get; set; }

        public string? JwtId { get; set; }

        public AuthSession AuthSession { get; set; } = null!;

        public RefreshTokenFamily RefreshTokenFamily { get; set; } = null!;

        public User User { get; set; } = null!;
    }
}

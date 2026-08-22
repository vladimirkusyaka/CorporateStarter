using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Core.Entities.Security
{
    public sealed class RefreshTokenFamily
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid AuthSessionId { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? RevokedAtUtc { get; set; }

        public string? RevokedByIp { get; set; }

        public string? ReuseDetectedByIp { get; set; }

        public DateTime? ReuseDetectedAtUtc { get; set; }

        public bool IsRevoked => RevokedAtUtc.HasValue;

        public User User { get; set; } = null!;

        public AuthSession AuthSession { get; set; } = null!;

        public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    }
}

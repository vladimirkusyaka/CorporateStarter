using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Shared.Dtos.Auth
{
    public sealed class MfaChallengeResponse
    {
        public Guid ChallengeId { get; set; }

        public string ProviderType { get; set; } = string.Empty;

        public string? PublicChallengeDataJson { get; set; }
    }
}

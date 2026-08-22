using CorporateStarter.Application.Common.Security.Mfa;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Interfaces.Security.Mfa
{
    public interface IMfaChallengeService
    {
        Task<MfaChallengeResult> CreateChallengeAsync(
            MfaEvaluationContext context,
            CancellationToken cancellationToken);

        Task<bool> VerifyChallengeAsync(
            Guid challengeId,
            string code,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken);
    }
}

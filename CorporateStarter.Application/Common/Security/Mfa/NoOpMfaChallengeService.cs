using CorporateStarter.Application.Common.Interfaces.Security.Mfa;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Security.Mfa
{
    public sealed class NoOpMfaChallengeService : IMfaChallengeService
    {
        public Task<MfaChallengeResult> CreateChallengeAsync(
            MfaEvaluationContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(MfaChallengeResult.NotRequired());
        }

        public Task<bool> VerifyChallengeAsync(
            Guid challengeId,
            string code,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }
    }
}

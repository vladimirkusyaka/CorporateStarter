using CorporateStarter.Application.Common.Interfaces.Security.Mfa;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Security.Mfa
{
    public sealed class NoOpMfaPolicyService : IMfaPolicyService
    {
        public Task<MfaRequirement> EvaluateRequirementAsync(
            MfaEvaluationContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(MfaRequirement.NotRequired);
        }
    }
}

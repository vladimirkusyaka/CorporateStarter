using CorporateStarter.Application.Common.Security.Mfa;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Interfaces.Security.Mfa
{
    public interface IMfaPolicyService
    {
        Task<MfaRequirement> EvaluateRequirementAsync(
            MfaEvaluationContext context,
            CancellationToken cancellationToken);
    }
}

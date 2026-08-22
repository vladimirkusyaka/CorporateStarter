using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Security.Mfa
{
    public enum MfaRequirement
    {
        NotRequired = 0,
        Required = 1,
        EnrollmentRequired = 2
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Security
{
    public sealed class PasswordPolicyValidationResult
    {
        private PasswordPolicyValidationResult(bool isValid, IReadOnlyList<string> errors)
        {
            IsValid = isValid;
            Errors = errors;
        }

        public bool IsValid { get; }

        public IReadOnlyList<string> Errors { get; }

        public static PasswordPolicyValidationResult Success()
        {
            return new PasswordPolicyValidationResult(true, []);
        }

        public static PasswordPolicyValidationResult Failure(IReadOnlyList<string> errors)
        {
            return new PasswordPolicyValidationResult(false, errors);
        }
    }
}

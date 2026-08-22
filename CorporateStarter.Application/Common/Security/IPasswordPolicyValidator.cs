using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Security
{
    public interface IPasswordPolicyValidator
    {
        PasswordPolicyValidationResult Validate(
            string password,
            string? login,
            string? email);
    }
}

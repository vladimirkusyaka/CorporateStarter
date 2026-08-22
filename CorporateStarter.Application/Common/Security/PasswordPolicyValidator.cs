using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Security
{
    public sealed class PasswordPolicyValidator : IPasswordPolicyValidator
    {
        private static readonly HashSet<string> CommonPasswords = new(
            StringComparer.OrdinalIgnoreCase)
        {
            "password",
            "password1",
            "password123",
            "admin",
            "admin123",
            "qwerty",
            "qwerty123",
            "letmein",
            "welcome",
            "welcome1",
            "changeme",
            "corporate",
            "company123"
        };

        private readonly PasswordPolicyOptions _options;

        public PasswordPolicyValidator(PasswordPolicyOptions options)
        {
            _options = options;
        }

        public PasswordPolicyValidationResult Validate(
            string password,
            string? login,
            string? email)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Password is required.");
                return PasswordPolicyValidationResult.Failure(errors);
            }

            if (password.Length < _options.MinimumLength)
            {
                errors.Add($"Password must be at least {_options.MinimumLength} characters long.");
            }

            if (password.Length > _options.MaximumLength)
            {
                errors.Add($"Password must be at most {_options.MaximumLength} characters long.");
            }

            if (_options.RequireUppercase && !password.Any(char.IsUpper))
            {
                errors.Add("Password must contain an uppercase letter.");
            }

            if (_options.RequireLowercase && !password.Any(char.IsLower))
            {
                errors.Add("Password must contain a lowercase letter.");
            }

            if (_options.RequireDigit && !password.Any(char.IsDigit))
            {
                errors.Add("Password must contain a digit.");
            }

            if (_options.RequireNonAlphanumeric && !password.Any(x => !char.IsLetterOrDigit(x)))
            {
                errors.Add("Password must contain a non-alphanumeric character.");
            }

            if (_options.RejectLoginInPassword && ContainsInsensitive(password, login))
            {
                errors.Add("Password must not contain the login.");
            }

            var emailLocalPart = GetEmailLocalPart(email);

            if (_options.RejectEmailLocalPartInPassword && ContainsInsensitive(password, emailLocalPart))
            {
                errors.Add("Password must not contain the email name.");
            }

            if (_options.RejectCommonPasswords && CommonPasswords.Contains(password.Trim()))
            {
                errors.Add("Password is too common.");
            }

            return errors.Count == 0
                ? PasswordPolicyValidationResult.Success()
                : PasswordPolicyValidationResult.Failure(errors);
        }

        private static bool ContainsInsensitive(string value, string? fragment)
        {
            if (string.IsNullOrWhiteSpace(fragment) || fragment.Length < 3)
            {
                return false;
            }

            return value.Contains(fragment, StringComparison.OrdinalIgnoreCase);
        }

        private static string? GetEmailLocalPart(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var atIndex = email.IndexOf('@');

            return atIndex > 0
                ? email[..atIndex]
                : email;
        }
    }
}
using System;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace CorporateStarter.Application.Security.Users.Validation
{
    internal static class UserValidation
    {
        public static bool IsValidLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                return false;

            return login.Length is >= 3 and <= 100;
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return new EmailAddressAttribute().IsValid(email);
        }

        public static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            return password.Length >= 8;
        }

        public static bool HasAtLeastOneRole(IReadOnlyList<Guid> roleIds)
        {
            return roleIds.Any(x => x != Guid.Empty);
        }
    }
}

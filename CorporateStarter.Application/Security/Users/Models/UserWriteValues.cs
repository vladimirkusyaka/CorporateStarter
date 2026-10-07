using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Application.Security.Users.Models
{
    public sealed class UserWriteValues
    {
        public bool ReplaceRoles { get; set; } = true;

        public Guid? Id { get; set; }

        public string Login { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        public string? Password { get; set; }

        public bool IsActive { get; set; } = true;

        public Guid? PersonId { get; set; }

        public IReadOnlyList<Guid> RoleIds { get; set; } = [];

        public void Normalize()
        {
            Login = (Login ?? string.Empty).Trim();
            Email = (Email ?? string.Empty).Trim().ToLowerInvariant();

            DisplayName = string.IsNullOrWhiteSpace(DisplayName)
                ? null
                : DisplayName.Trim();

            Password = string.IsNullOrWhiteSpace(Password)
                ? null
                : Password.Trim();

            if (PersonId == Guid.Empty)
                PersonId = null;

            RoleIds = (RoleIds ?? [])
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToArray();
        }
    }
}

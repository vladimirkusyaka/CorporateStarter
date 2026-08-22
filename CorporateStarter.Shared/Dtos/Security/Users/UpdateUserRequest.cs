using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.Security.Users
{
    public sealed class UpdateUserRequest
    {
        public string Login { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        public bool IsActive { get; set; } = true;

        public Guid? PersonId { get; set; }

        public IReadOnlyList<Guid> RoleIds { get; set; } = [];
    }
}

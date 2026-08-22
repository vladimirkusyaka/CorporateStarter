using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.Auth
{
    public sealed class UserRoleProfileDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public IReadOnlyList<UserPermissionProfileDto> Permissions { get; set; } = [];
    }
}

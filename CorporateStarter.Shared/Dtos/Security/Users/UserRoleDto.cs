using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.Security.Users
{
    public sealed class UserRoleDto
    {
        public Guid RoleId { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}

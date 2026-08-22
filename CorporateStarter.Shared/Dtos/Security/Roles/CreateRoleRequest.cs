using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.Security.Roles
{
    public sealed class CreateRoleRequest
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public IReadOnlyList<Guid> PermissionIds { get; set; } = [];
    }
}

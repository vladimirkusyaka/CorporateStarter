using System;
using System.Text;
using System.Collections.Generic;
using CorporateStarter.Shared.Dtos.Permissions;

namespace CorporateStarter.Shared.Dtos.Security.Roles
{
    public sealed class RoleDetailsDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsSystemRole { get; set; }

        public bool IsActive { get; set; }

        public IReadOnlyList<PermissionListItemDto> Permissions { get; set; } = [];
    }
}

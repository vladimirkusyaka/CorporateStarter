using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.Permissions
{
    public sealed class PermissionListItemDto
    {
        public Guid Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Group { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}

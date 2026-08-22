using System;
using System.Text;
using System.Collections.Generic;
using CorporateStarter.Core.Entities.Common;

namespace CorporateStarter.Core.Entities.Security
{
    public class Permission : BaseEntity
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Group { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}

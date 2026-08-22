using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Core.Entities.Security
{
    public class RolePermission
    {
        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public Guid PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;
    }
}

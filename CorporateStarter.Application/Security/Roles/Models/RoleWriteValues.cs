using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Application.Security.Roles.Models
{
    public sealed class RoleWriteValues
    {
        public Guid? Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public IReadOnlyList<Guid> PermissionIds { get; set; } = [];

        public void Normalize()
        {
            Name = Name.Trim();
            Description = string.IsNullOrWhiteSpace(Description)
                ? null
                : Description.Trim();

            PermissionIds = PermissionIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToArray();
        }
    }
}

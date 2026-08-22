using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.Auth
{
    public sealed class UserPermissionProfileDto
    {
        public Guid Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Group { get; set; } = string.Empty;
    }
}

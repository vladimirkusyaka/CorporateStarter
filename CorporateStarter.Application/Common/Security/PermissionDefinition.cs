using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Application.Common.Security
{
    public sealed record PermissionDefinition(
        string Code,
        string Name,
        string Group,
        string? Description = null);
}

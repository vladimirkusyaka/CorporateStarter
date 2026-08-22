using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.Security.Users
{
    public sealed class ChangeUserPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}

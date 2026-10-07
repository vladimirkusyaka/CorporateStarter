using System.ComponentModel.DataAnnotations;
using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.Security.Users
{
    public sealed class CreateUserRequest
    {
        [Required, StringLength(100, MinimumLength = 3)]
        public string Login { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(256)]
        public string? DisplayName { get; set; }

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public Guid? PersonId { get; set; }

        public IReadOnlyList<Guid> RoleIds { get; set; } = [];
    }
}

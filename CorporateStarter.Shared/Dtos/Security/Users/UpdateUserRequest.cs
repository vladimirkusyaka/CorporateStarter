using System.ComponentModel.DataAnnotations;
using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.Security.Users
{
    public sealed class UpdateUserRequest
    {
        [Required, StringLength(100, MinimumLength = 3)]
        public string Login { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(256)]
        public string? DisplayName { get; set; }

        public bool IsActive { get; set; } = true;

        public Guid? PersonId { get; set; }

        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<Guid>? RoleIds { get; set; } // null preserves existing assignments; [] clears them.
    }
}

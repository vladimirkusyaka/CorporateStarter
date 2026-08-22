using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.Audit
{
    public class AuditLogListItemDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string EntityName { get; set; } = string.Empty;

        public string EntityId { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string? UserEmail { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public string? OldValuesJson { get; set; }

        public string? NewValuesJson { get; set; }

    }
}

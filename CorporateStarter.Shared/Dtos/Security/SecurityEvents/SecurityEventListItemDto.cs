using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Shared.Dtos.Security.SecurityEvents
{
    public sealed class SecurityEventListItemDto
    {
        public Guid Id { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string Severity { get; set; } = string.Empty;

        public string Outcome { get; set; } = string.Empty;

        public Guid? SubjectUserId { get; set; }

        public string? SubjectUserEmail { get; set; }

        public Guid? AuthSessionId { get; set; }

        public Guid? RefreshTokenFamilyId { get; set; }

        public string? CorrelationId { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public string? DetailsJson { get; set; }
    }
}

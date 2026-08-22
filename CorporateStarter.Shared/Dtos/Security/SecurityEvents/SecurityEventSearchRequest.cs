using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Shared.Dtos.Security.SecurityEvents
{
    public sealed class SecurityEventSearchRequest
    {
        public string? EventType { get; set; }

        public string? Severity { get; set; }

        public string? Outcome { get; set; }

        public Guid? SubjectUserId { get; set; }

        public Guid? AuthSessionId { get; set; }

        public Guid? RefreshTokenFamilyId { get; set; }

        public string? CorrelationId { get; set; }

        public DateTime? FromUtc { get; set; }

        public DateTime? ToUtc { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 50;
    }
}

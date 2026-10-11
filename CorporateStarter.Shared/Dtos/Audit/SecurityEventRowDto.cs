namespace CorporateStarter.Shared.Dtos.Audit;

// Compact table result; full context is fetched only when details are opened.
public sealed class SecurityEventRowDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string EventType { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Outcome { get; set; } = "";
    public string? SubjectUserEmail { get; set; }
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
}

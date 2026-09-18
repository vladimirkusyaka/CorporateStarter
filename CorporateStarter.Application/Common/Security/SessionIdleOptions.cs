namespace CorporateStarter.Application.Common.Security;

public sealed class SessionIdleOptions
{
    public const string SectionName = "SessionIdle";
    public int TimeoutMinutes { get; set; } = 30;
    public bool IsValid() => TimeoutMinutes is >= 1 and <= 1440;
}

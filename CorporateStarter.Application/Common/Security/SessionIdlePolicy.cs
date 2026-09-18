namespace CorporateStarter.Application.Common.Security;

public sealed class SessionIdlePolicy
{
    public TimeSpan Timeout { get; }

    public SessionIdlePolicy(SessionIdleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.IsValid())
            throw new ArgumentOutOfRangeException(nameof(options),
                "SessionIdle:TimeoutMinutes must be between 1 and 1440.");

        Timeout = TimeSpan.FromMinutes(options.TimeoutMinutes);
    }

    public bool IsExpired(DateTime lastActivityUtc, DateTime nowUtc) =>
        lastActivityUtc <= nowUtc - Timeout;
}

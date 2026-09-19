namespace CorporateStarter.Application.Common.Security;

public sealed class SessionIdlePolicy
{
    public TimeSpan Timeout { get; }
    public bool IsEnabled => Timeout > TimeSpan.Zero;

    public SessionIdlePolicy(SessionIdleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.IsValid())
            throw new ArgumentOutOfRangeException(nameof(options),
                "SessionIdle:TimeoutMinutes must be between 0 and 1440.");

        Timeout = TimeSpan.FromMinutes(options.TimeoutMinutes);
    }

    public bool IsExpired(DateTime lastActivityUtc, DateTime nowUtc) =>
    IsEnabled && lastActivityUtc <= nowUtc - Timeout;
}

using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Shared.Dtos.Auth;
namespace CorporateStarter.Client.Core.Auth;

// Scoped per browser application / server circuit. Never share between users.
public sealed class ClientPermissions(IClientAuthState auth) : IDisposable
{
    private sealed record Grant(Guid UserId, long Generation, IReadOnlySet<string> Codes);
    private volatile Grant? _grant;
    public event Action? Changed;

    private Grant? CurrentGrant
    {
        get
        {
            var grant = _grant;
            var session = auth.Current;
            return grant is not null && grant.UserId == session.UserId && grant.Generation == session.SessionGeneration
                ? grant : null;
        }
    }
    public bool IsLoaded => CurrentGrant is not null;
    public bool Has(string code) => CurrentGrant?.Codes.Contains(code) == true;

    public void Apply(UserProfileDto profile, ClientAuthSnapshot start)
    {
        var current = auth.Current;
        if (profile.Id != start.UserId || start.UserId != current.UserId ||
            start.SessionGeneration != current.SessionGeneration) return;
        // Replace the complete immutable snapshot; readers never observe a partially cleared set.
        _grant = new(profile.Id, start.SessionGeneration, profile.Roles.SelectMany(x => x.Permissions)
            .Select(x => x.Code).ToHashSet(StringComparer.Ordinal));
        Changed?.Invoke();
    }
    public void Clear() { _grant = null; Changed?.Invoke(); }
    public void Dispose() { _grant = null; Changed = null; }
}

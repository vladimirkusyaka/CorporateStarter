namespace CorporateStarter.Shared.Common;

// One naming convention shared by the API and both clients.
public static class EntityPermissions
{
    public static TableCapabilities Capabilities(string resource, Func<string, bool> has) => new(
        has(resource + ".create"), has(resource + ".update"), has(resource + ".delete"),
        has(resource + ".view_inactive"), has(resource + ".restore"));

    // Use the saved status, so an unsaved toggle can always be undone.
    public static bool CanChangeStatus(this TableCapabilities caps, bool originalActive) =>
        caps.ViewInactive && (originalActive ? caps.CanDelete : caps.CanRestore);

    public static void RequireStatusChange(this TableCapabilities caps, bool originalActive, bool requestedActive)
    {
        if (originalActive != requestedActive && !caps.CanChangeStatus(originalActive))
            throw new UnauthorizedAccessException("Changing this status is not permitted.");
    }
}

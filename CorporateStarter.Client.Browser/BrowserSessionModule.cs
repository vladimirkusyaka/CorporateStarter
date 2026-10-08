namespace CorporateStarter.Client.Browser;

// All consumers must import the exact same URL: the ES module owns the shared session state.
// Change the version whenever browser-session.js changes (current value: SHA-256 prefix).
internal static class BrowserSessionModule
{
    public const string Path =
        "./_content/CorporateStarter.Client.Browser/auth/browser-session.js?v=1e9e296c7e061fe5";
}

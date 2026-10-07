namespace CorporateStarter.Client.UI.Components.Lookups;

public sealed record SelectionOption(Guid Id, string Label, string Group, bool Enabled = true);
namespace CorporateStarter.Shared.Common;

public sealed record TableCapabilities(bool CanCreate, bool CanUpdate, bool CanDelete, bool ViewInactive, bool CanRestore);

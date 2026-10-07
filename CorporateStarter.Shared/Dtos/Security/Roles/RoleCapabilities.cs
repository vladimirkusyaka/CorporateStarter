using CorporateStarter.Shared.Common;
namespace CorporateStarter.Shared.Dtos.Security.Roles;

public sealed record RoleCapabilities(TableCapabilities Table, bool CanManagePermissions);

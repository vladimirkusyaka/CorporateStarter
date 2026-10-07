using CorporateStarter.Shared.Common;
namespace CorporateStarter.Shared.Dtos.Security.Users;
public sealed record UserCapabilities(TableCapabilities Table, bool CanChangePassword);

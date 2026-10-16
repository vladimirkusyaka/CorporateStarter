using CorporateStarter.Shared.Common;
namespace CorporateStarter.Application.Common.Security;

public interface IEntityAccess
{
    TableCapabilities For(string resource);
}

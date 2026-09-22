using System.Reflection;
namespace CorporateStarter.Client.UI;

public static class ClientUiAssembly
{
    public static IReadOnlyList<Assembly> RouteAssemblies { get; } = [typeof(ClientUiAssembly).Assembly];
}

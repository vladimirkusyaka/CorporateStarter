using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CorporateStarter.Client.UI.Components
{
    public partial class ClientRoutes
    {
        [Parameter, EditorRequired] public Assembly HostAssembly { get; set; } = default!;
    }
}

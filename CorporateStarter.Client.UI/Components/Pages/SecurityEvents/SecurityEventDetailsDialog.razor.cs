using CorporateStarter.Client.Core.Audit;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CorporateStarter.Client.UI.Components.Pages.SecurityEvents
{
    public partial class SecurityEventDetailsDialog
    {
        [Inject] SecurityEventsClient Client { get; set; } = default!;
        [Parameter] public Guid EventId { get; set; }
    }
}

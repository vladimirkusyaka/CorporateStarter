using Microsoft.AspNetCore.Components;
using CorporateStarter.Client.Core.Serialization;

namespace CorporateStarter.Client.UI.Components.Details
{
    public partial class JsonDetails
    {
        [Parameter] public string? Value { get; set; }
    }
}

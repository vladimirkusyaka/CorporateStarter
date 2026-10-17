using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.JSInterop;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CorporateStarter.Client.UI.Components.Details
{
    public partial class CopyIdentifier
    {
        [Inject] IJSRuntime Js { get; set; }
        [Parameter] public string? Value { get; set; }
        [Parameter] public string Label { get; set; } = "identifier";
        private string? _status;
        private async Task CopyAsync()
        {
            if (string.IsNullOrWhiteSpace(Value)) return;
            try
            {
                await Js.InvokeVoidAsync("navigator.clipboard.writeText", Value);
                _status = "Copied";
            }
            catch (JSException)
            {
                _status = "Could not copy. Select and copy the identifier manually.";
            }
        }
    }
}

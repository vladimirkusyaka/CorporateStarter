using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.UI.Components.Editors
{
    public partial class EditorDialogFrame
    {
        [Parameter] public string Title { get; set; } = "Edit record";
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public bool Loading { get; set; }
        [Parameter] public bool Loaded { get; set; }
        [Parameter] public bool Saving { get; set; }
        [Parameter] public bool Blocked { get; set; }
        [Parameter] public bool SessionReady { get; set; }
        [Parameter] public bool Uncertain { get; set; }
        [Parameter] public bool ConfirmDiscard { get; set; }
        [Parameter] public string? Error { get; set; }
        [Parameter] public EventCallback Close { get; set; }
        [Parameter] public EventCallback Save { get; set; }
        [Parameter] public EventCallback Retry { get; set; }
        [Parameter] public EventCallback KeepEditing { get; set; }
        [Parameter] public EventCallback Discard { get; set; }
        private MudForm? _form;
        public async Task<bool> ValidateAsync()
        {
            if (_form is null) return false;
            await _form.ValidateAsync();
            return _form.IsValid;
        }
    }
}

using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.UI.Localization
{
    public partial class LanguagePicker
    {
        [Parameter] public bool Disabled { get; set; }
        private void Select(string language) => L.Select(language);
    }
}

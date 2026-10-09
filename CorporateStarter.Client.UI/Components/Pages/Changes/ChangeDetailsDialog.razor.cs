using Microsoft.AspNetCore.Components;
using CorporateStarter.Client.Core.Audit;

namespace CorporateStarter.Client.UI.Components.Pages.Changes
{
    public partial class ChangeDetailsDialog
    {
        [Parameter] public Guid ChangeId { get; set; }

        [Inject] private ChangesClient Client { get; set; } = default!;
        private bool _onlyChanged = true;
    }
}

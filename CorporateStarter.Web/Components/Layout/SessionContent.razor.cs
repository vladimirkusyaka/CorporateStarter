using Microsoft.AspNetCore.Components;

namespace CorporateStarter.Web.Components.Layout
{
    public partial class SessionContent
    {
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public bool CanRender { get; set; }

        protected override bool ShouldRender() => CanRender;
    }
}

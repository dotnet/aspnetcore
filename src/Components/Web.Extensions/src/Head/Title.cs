using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    public class Title : HeadElementBase
    {
        [Parameter]
        public string Value { get; set; } = string.Empty;

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        internal override object ElementKey => "title";

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            ChildContent?.Invoke(builder);
        }

        internal override ValueTask SaveInitialStateAsync()
        {
            return ValueTask.CompletedTask;
        }

        internal override async ValueTask ApplyChangesAsync()
        {
            await HeadManager.SetTitleAsync(Value);
        }

        public override string ToString()
        {
            return Value;
        }
    }
}

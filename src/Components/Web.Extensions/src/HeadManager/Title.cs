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

        internal override async ValueTask<object> GetInitialStateAsync()
        {
            return await HeadManager.GetTitleAsync();
        }

        internal override ValueTask ResetInitialStateAsync(object initialState)
        {
            return HeadManager.SetTitleAsync(initialState);
        }

        internal override async ValueTask ApplyAsync()
        {
            await HeadManager.SetTitleAsync(Value);
        }

        public override string ToString()
        {
            return Value;
        }
    }
}

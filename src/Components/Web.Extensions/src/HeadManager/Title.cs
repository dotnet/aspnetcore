using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    public class Title : HeadElementBase
    {
        internal override object ElementKey => "title";

        [Parameter]
        public string Value { get; set; } = string.Empty;

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
    }
}

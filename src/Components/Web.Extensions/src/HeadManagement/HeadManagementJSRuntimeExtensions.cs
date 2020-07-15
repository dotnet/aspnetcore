using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal static class HeadManagementJSRuntimeExtensions
    {
        private const string JsFunctionsPrefix = "_blazorHeadManager";

        public static ValueTask SetTitleAsync(this IJSRuntime jsRuntime, string title)
        {
            return jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.setTitle", title);
        }

        public static ValueTask ApplyTagAsync(this IJSRuntime jsRuntime, TagElement tag, string id)
        {
            return jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.applyHeadTag", tag, id);
        }

        public static ValueTask RemoveTagAsync(this IJSRuntime jsRuntime, string id)
        {
            return jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.removeHeadTag", id);
        }
    }
}

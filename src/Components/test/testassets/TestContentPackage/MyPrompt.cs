using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace TestContentPackage
{
    public static class MyPrompt
    {
        public static ValueTask<string> Show(IJSRuntime jsRuntime, string message)
        {
            return jsRuntime.InvokeAsync<string>(
                "TestContentPackage.showPrompt", // Keep in sync with identifiers in the.js file
                message);
        }
    }
}

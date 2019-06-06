using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace BlazorLibrary_CSharp
{
    public class ExampleJsInterop
    {
        public static Task<string> Prompt(IJSRuntime jsRuntime, string message)
        {
            // Implemented in exampleJsInterop.js
            return jsRuntime.InvokeAsync<string>(
                "exampleJsFunctions.showPrompt",
                message);
        }
    }
}

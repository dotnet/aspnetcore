using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Company.RazorClassLibrary1
{
    // This class provides an example of how JavaScript functionality can be wrapped
    // in a .NET class for easy consumption. The associated JavaScript module is
    // loaded on demand when the class is instantiated.
    //
    // This class can be registered as scoped DI service and then injected into Blazor
    // components for use.

    public class ExampleJsInterop : IAsyncDisposable
    {
        private readonly Task<IJSObjectReference> moduleTask;

        public ExampleJsInterop(IJSRuntime jsRuntime)
        {
            moduleTask = jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Company.RazorClassLibrary1/exampleJsInterop.js").AsTask();
        }

        public async ValueTask<string> Prompt(string message)
        {
            var module = await moduleTask;
            return await module.InvokeAsync<string>("showPrompt", message);
        }

        public async ValueTask DisposeAsync()
        {
            var module = await moduleTask;
            await module.DisposeAsync();
        }
    }
}

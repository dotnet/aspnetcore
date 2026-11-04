using Microsoft.JSInterop;

namespace Company.RazorClassLibrary1;

// This class provides an example of how JavaScript functionality can be wrapped
// in a .NET class for easy consumption. The associated JavaScript module is
// loaded on demand when first needed.
//
// This class can be registered as scoped DI service and then injected into Blazor
// components for use.

public class ExampleJsInterop(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask = new(async () =>
    {
        try
        {
            return await jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Company.RazorClassLibrary1/exampleJsInterop.js").AsTask();
        }
        catch (JSException ex)
        {
            throw new InvalidOperationException("Unable to import the JavaScript module.", ex);
        }
    });

    public async ValueTask<string> Prompt(string message)
    {
        var module = await moduleTask.Value;

        try
        {
            return await module.InvokeAsync<string>("showPrompt", message);
        }
        catch (JSException ex)
        {
            throw new InvalidOperationException("Unable to invoke the showPrompt JavaScript function.", ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}

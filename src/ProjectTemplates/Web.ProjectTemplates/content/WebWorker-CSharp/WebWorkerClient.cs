using Microsoft.JSInterop;

namespace Company.WebWorker1;

// This class provides a client for communicating with a Web Worker running
// .NET code. The associated JavaScript module is loaded on demand when the
// worker is created.
//
// Worker methods are static methods marked with [JSExport] in a static partial
// class. Due to [JSExport] limitations, worker methods can only return primitives
// or strings. For complex types, serialize to JSON before returning — it will be
// automatically deserialized.
//
// Example worker class:
//
//     [SupportedOSPlatform("browser")]
//     public static partial class MyWorker
//     {
//         [JSExport]
//         public static string Process(string input) => $"Processed: {input}";
//     }
//
// Example usage:
//
//     var worker = await WebWorkerClient.CreateAsync(JSRuntime);
//     var result = await worker.InvokeAsync<string>("MyApp.MyWorker.Process", ["Hello"]);

public sealed class WebWorkerClient(IJSObjectReference worker) : IAsyncDisposable
{
    public static async Task<WebWorkerClient> CreateAsync(IJSRuntime jsRuntime)
    {
        await using var module = await jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Company.WebWorker1/dotnet-web-worker-client.js");

        var workerRef = await module.InvokeAsync<IJSObjectReference>("create");

        return new WebWorkerClient(workerRef);
    }

    public async Task<TResult> InvokeAsync<TResult>(string method, object[] args, CancellationToken cancellationToken = default)
    {
        return await worker.InvokeAsync<TResult>("invoke", cancellationToken, [method, args]);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await worker.InvokeVoidAsync("terminate");
        }
        catch (JSDisconnectedException)
        {
            // JS interop disconnected, worker is already gone
        }

        await worker.DisposeAsync();
    }
}

using Microsoft.JSInterop;

namespace Company.WebWorker1;

public sealed class WebWorkerClient(IJSObjectReference worker) : IAsyncDisposable
{
    private const int DefaultTimeoutMs = 60000;

    public static async Task<WebWorkerClient> CreateAsync(IJSRuntime jsRuntime, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
        await using var module = await jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", cancellationToken, "./_content/Company.WebWorker1/dotnet-web-worker-client.js");

        var workerRef = await module.InvokeAsync<IJSObjectReference>("create", cancellationToken, timeoutMs);

        return new WebWorkerClient(workerRef);
    }

    // Invokes a [JSExport] method from the web worker.
    // The method string is the fully qualified path: "AssemblyName.ClassName.MethodName".
    // Arguments and return values must be primitive types or strings.
    public async Task<TResult> InvokeAsync<TResult>(string method, object[] args, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
        return await worker.InvokeAsync<TResult>("invoke", cancellationToken, [method, args, timeoutMs]);
    }

    public async Task InvokeVoidAsync(string method, object[] args, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
        await worker.InvokeVoidAsync("invoke", cancellationToken, [method, args, timeoutMs]);
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

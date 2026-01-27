// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Microsoft.JSInterop;

namespace WebWorkerTemplate.WorkerClient;

[SupportedOSPlatform("browser")]
public sealed class WorkerClient(IJSObjectReference module, IJSObjectReference worker) : IAsyncDisposable
{
    public static async Task<WorkerClient> CreateAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken = default)
    {
        var module = await jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            cancellationToken,
            "./_content/WebWorkerTemplate.WorkerClient/worker-client.js");

        var worker = await module.InvokeAsync<IJSObjectReference>("createWorker", cancellationToken);
        return new WorkerClient(module, worker);
    }

    public ValueTask<string> GreetAsync(string name, CancellationToken cancellationToken = default)
        => worker.InvokeAsync<string>("greet", cancellationToken, name);

    public ValueTask TerminateAsync()
        => worker.InvokeVoidAsync("terminate");

    public async ValueTask DisposeAsync()
    {
        await worker.DisposeAsync();
        await module.DisposeAsync();
    }
}

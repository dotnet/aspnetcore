// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Company.WorkerClassLibrary1;

/// <summary>
/// Client for communicating with a WebWorker running .NET code.
/// Initialize once, then call worker methods dynamically.
/// </summary>
public sealed class WorkerClient : IWorkerClient, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;

    public WorkerClient(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private void EnsureInitialized()
    {
        if (_module is null)
        {
            throw new InvalidOperationException("WorkerClient is not initialized. Call InitializeAsync first.");
        }
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (_module is not null)
        {
            return;
        }

        _module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Company.WorkerClassLibrary1/worker-client.js");

        await _module.InvokeVoidAsync("createWorker");
    }

    /// <inheritdoc />
    public void Terminate()
    {
        EnsureInitialized();
        ((IJSInProcessObjectReference)_module!).InvokeVoid("terminate");
    }

    /// <inheritdoc />
    public async Task WaitForReadyAsync()
    {
        EnsureInitialized();
        await _module!.InvokeVoidAsync("waitForReady");
    }

    /// <inheritdoc />
    public async Task<string> InvokeStringAsync(string method, TimeSpan timeout, params object[] args)
    {
        EnsureInitialized();
        var workerTask = _module!.InvokeAsync<string>("invokeString", method, args).AsTask();
        return timeout == Timeout.InfiniteTimeSpan
            ? await workerTask
            : await workerTask.WaitAsync(timeout);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
            _module = null;
        }
    }
}

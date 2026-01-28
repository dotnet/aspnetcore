// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Company.WebWorker1;

/// <summary>
/// Client for communicating with a Web Worker running .NET code.
/// </summary>
/// <remarks>
/// <para>
/// Worker methods must be static methods marked with <c>[JSExport]</c> in a <c>static partial class</c>.
/// The project containing worker methods requires <c>&lt;AllowUnsafeBlocks&gt;true&lt;/AllowUnsafeBlocks&gt;</c>.
/// </para>
/// <para>
/// Example worker class:
/// <code>
/// [SupportedOSPlatform("browser")]
/// public static partial class MyWorker
/// {
///     [JSExport]
///     public static string Process(string input) => $"Processed: {input}";
/// }
/// </code>
/// </para>
/// <para>
/// Example usage:
/// <code>
/// @inject IJSRuntime JSRuntime
///
/// private WebWorkerClient? _worker;
///
/// protected override async Task OnAfterRenderAsync(bool firstRender)
/// {
///     if (firstRender)
///     {
///         _worker = await WebWorkerClient.CreateAsync(JSRuntime);
///     }
/// }
///
/// async Task CallWorker()
/// {
///     var result = await _worker!.InvokeStringAsync("MyApp.MyWorker.Process", ["Hello"]);
/// }
///
/// public async ValueTask DisposeAsync() => await (_worker?.DisposeAsync() ?? ValueTask.CompletedTask);
/// </code>
/// </para>
/// </remarks>
public sealed class WebWorkerClient(IJSObjectReference worker) : IAsyncDisposable
{
    /// <summary>
    /// Creates and initializes a new .NET Web Worker client instance.
    /// </summary>
    /// <param name="jsRuntime">The JS runtime instance.</param>
    /// <returns>A ready-to-use WebWorkerClient instance.</returns>
    /// <exception cref="JSException">Thrown if the worker fails to initialize.</exception>
    public static async Task<WebWorkerClient> CreateAsync(IJSRuntime jsRuntime)
    {
        await using var module = await jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Company.WebWorker1/dotnet-web-worker-client.js");

        var workerRef = await module.InvokeAsync<IJSObjectReference>("create");

        return new WebWorkerClient(workerRef);
    }

    /// <summary>
    /// Invokes a method on the worker and returns the result as a string.
    /// </summary>
    /// <param name="method">Full method path: "Namespace.ClassName.MethodName"</param>
    /// <param name="args">Arguments to pass to the method.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The string result from the worker method.</returns>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    /// <exception cref="JSException">Thrown if the worker method throws an exception.</exception>
    public async Task<string> InvokeStringAsync(string method, object[] args, CancellationToken cancellationToken = default)
    {
        return await worker.InvokeAsync<string>("invokeString", cancellationToken, [method, args]);
    }

    /// <summary>
    /// Terminates the worker and releases resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await worker.InvokeVoidAsync("terminate");
        }
        catch (JSDisconnectedException)
        {
            // Circuit disconnected, worker is already gone
        }

        await worker.DisposeAsync();
    }
}

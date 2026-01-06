// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace BlazorWebCSharp._1.WorkerClient;

/// <summary>
/// Client for communicating with a WebWorker running .NET code.
/// Initialize once, then call worker methods dynamically.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class WorkerClient
{
    /// <summary>
    /// Default timeout for worker method invocations.
    /// Set to <see cref="Timeout.InfiniteTimeSpan"/> to disable timeout.
    /// </summary>
    public static TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

    private static bool _initialized;

    /// <summary>
    /// Initializes the worker client. Must be called before invoking any worker methods.
    /// </summary>
    public static async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        await JSHost.ImportAsync(
            moduleName: nameof(WorkerClient),
            moduleUrl: $"../_content/BlazorWebCSharp.1.WorkerClient/worker-client.js");
    }

    /// <summary>
    /// Waits for the worker's .NET runtime to be fully initialized.
    /// Call this after InitializeAsync and before the first InvokeAsync to ensure the worker is ready.
    /// </summary>
    /// <returns>A task that completes when the worker is ready</returns>
    /// <exception cref="InvalidOperationException">Thrown if InitializeAsync was not called</exception>
    /// <exception cref="JSException">Thrown if the worker failed to initialize</exception>
    public static async Task WaitForReadyAsync()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("WorkerClient is not initialized. Call InitializeAsync first.");
        }

        await WaitForReadyInternal();
    }

    [JSImport("waitForReady", nameof(WorkerClient))]
    private static partial Task WaitForReadyInternal();

    [JSImport("invoke", nameof(WorkerClient))]
    private static partial Task<JSObject> InvokeInternal(string method, [JSMarshalAs<JSType.Array<JSType.Any>>] object[] args);

    /// <summary>
    /// Invokes a method on the worker and returns the result as a byte array.
    /// Uses <see cref="DefaultTimeout"/> for timeout.
    /// </summary>
    /// <param name="method">Full method path: "Namespace.ClassName.MethodName"</param>
    /// <param name="args">Arguments to pass to the method</param>
    /// <returns>Result as byte array</returns>
    /// <exception cref="InvalidOperationException">Thrown if InitializeAsync was not called</exception>
    /// <exception cref="JSException">Thrown if the worker method throws an exception</exception>
    /// <exception cref="TimeoutException">Thrown if the worker method exceeds the default timeout</exception>
    public static Task<byte[]> InvokeAsync(string method, params object[] args)
    {
        return InvokeAsync(method, DefaultTimeout, args);
    }

    /// <summary>
    /// Invokes a method on the worker with a specific timeout and returns the result as a byte array.
    /// </summary>
    /// <param name="method">Full method path: "Namespace.ClassName.MethodName"</param>
    /// <param name="timeout">Maximum time to wait for the worker to complete. Use <see cref="Timeout.InfiniteTimeSpan"/> to disable.</param>
    /// <param name="args">Arguments to pass to the method</param>
    /// <returns>Result as byte array</returns>
    /// <exception cref="InvalidOperationException">Thrown if InitializeAsync was not called</exception>
    /// <exception cref="JSException">Thrown if the worker method throws an exception</exception>
    /// <exception cref="TimeoutException">Thrown if the worker method exceeds the specified timeout</exception>
    public static async Task<byte[]> InvokeAsync(string method, TimeSpan timeout, params object[] args)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("WorkerClient is not initialized. Call InitializeAsync first.");
        }

        Task<JSObject> workerTask = InvokeInternal(method, args);

        // No timeout requested
        if (timeout == Timeout.InfiniteTimeSpan)
        {
            var result = await workerTask;
            return GetBytesFromJSObject(result);
        }

        // Race between worker task and timeout
        Task completedTask = await Task.WhenAny(workerTask, Task.Delay(timeout));

        if (completedTask != workerTask)
        {
            throw new TimeoutException($"Worker method '{method}' did not complete within {timeout.TotalSeconds:F1} seconds.");
        }

        var resultObj = await workerTask;
        return GetBytesFromJSObject(resultObj);
    }

    /// <summary>
    /// Invokes a method on the worker with cancellation support and returns the result as a byte array.
    /// </summary>
    /// <param name="method">Full method path: "Namespace.ClassName.MethodName"</param>
    /// <param name="cancellationToken">Token to cancel waiting for the result (does not stop worker execution)</param>
    /// <param name="args">Arguments to pass to the method</param>
    /// <returns>Result as byte array</returns>
    /// <exception cref="InvalidOperationException">Thrown if InitializeAsync was not called</exception>
    /// <exception cref="JSException">Thrown if the worker method throws an exception</exception>
    /// <exception cref="OperationCanceledException">Thrown if the cancellation token is triggered</exception>
    public static async Task<byte[]> InvokeAsync(string method, CancellationToken cancellationToken, params object[] args)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("WorkerClient is not initialized. Call InitializeAsync first.");
        }

        Task<JSObject> workerTask = InvokeInternal(method, args);

        // Create a task that completes when cancellation is requested
        var tcs = new TaskCompletionSource<bool>();
        using var registration = cancellationToken.Register(() => tcs.TrySetResult(true));

        Task completedTask = await Task.WhenAny(workerTask, tcs.Task);

        if (completedTask != workerTask)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        var result = await workerTask;
        return GetBytesFromJSObject(result);
    }

    /// <summary>
    /// Terminates the current worker and creates a new one.
    /// All pending requests will be rejected with an error.
    /// Use this to recover from a stuck or unresponsive worker.
    /// </summary>
    /// <remarks>
    /// After calling Terminate, the worker will be automatically recreated.
    /// The next InvokeAsync call will use the new worker instance.
    /// Note: This is an expensive operation as it requires reloading the .NET runtime in the worker.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if InitializeAsync was not called</exception>
    public static void Terminate()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("WorkerClient is not initialized. Call InitializeAsync first.");
        }

        TerminateInternal();
    }

    [JSImport("terminate", nameof(WorkerClient))]
    private static partial void TerminateInternal();

    [JSImport("globalThis.Uint8Array.prototype.slice.call")]
    private static partial byte[] GetBytesFromJSObject(JSObject obj);
}

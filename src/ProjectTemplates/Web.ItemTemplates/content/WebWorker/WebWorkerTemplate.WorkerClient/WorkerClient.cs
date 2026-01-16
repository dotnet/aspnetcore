// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace WebWorkerTemplate.WorkerClient;

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

        await JSHost.ImportAsync(
            moduleName: nameof(WorkerClient),
            moduleUrl: $"../_content/WebWorkerTemplate.WorkerClient/worker-client.js");

        CreateWorkerInternal();

        _initialized = true;
    }

    [JSImport("createWorker", nameof(WorkerClient))]
    private static partial void CreateWorkerInternal();

    [JSImport("setProgressCallback", nameof(WorkerClient))]
    private static partial void SetProgressCallbackInternal([JSMarshalAs<JSType.Function<JSType.String, JSType.Number, JSType.Number>>] Action<string, int, int> callback);

    [JSImport("clearProgressCallback", nameof(WorkerClient))]
    private static partial void ClearProgressCallbackInternal();

    /// <summary>
    /// Sets a callback to receive progress updates from worker operations.
    /// </summary>
    /// <param name="callback">Callback function receiving (message, current, total), or null to clear</param>
    public static void SetProgressCallback(Action<string, int, int>? callback)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("WorkerClient is not initialized. Call InitializeAsync first.");
        }

        if (callback == null)
        {
            ClearProgressCallbackInternal();
        }
        else
        {
            SetProgressCallbackInternal(callback);
        }
    }

    /// <summary>
    /// Terminates the current worker and creates a new one.
    /// All pending requests will be rejected with an error.
    /// Use this to recover from a stuck or unresponsive worker.
    /// </summary>
    /// <remarks>
    /// After calling Terminate, the worker will be automatically recreated.
    /// The next InvokeStringAsync call will use the new worker instance.
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

    [JSImport("waitForReady", nameof(WorkerClient))]
    private static partial Task WaitForReadyInternal();

    /// <summary>
    /// Waits for the worker to be fully initialized and ready.
    /// </summary>
    /// <returns>A task that completes when the worker is ready</returns>
    /// <exception cref="InvalidOperationException">Thrown if InitializeAsync was not called</exception>
    public static async Task WaitForReadyAsync()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("WorkerClient is not initialized. Call InitializeAsync first.");
        }

        await WaitForReadyInternal();
    }

    [JSImport("invokeString", nameof(WorkerClient))]
    private static partial Task<string> InvokeStringInternal(string method, [JSMarshalAs<JSType.Array<JSType.Any>>] object[] args);

    /// <summary>
    /// Invokes a method on the worker that returns a string.
    /// Uses <see cref="DefaultTimeout"/> for timeout.
    /// </summary>
    /// <param name="method">Full method path: "Namespace.ClassName.MethodName"</param>
    /// <param name="args">Arguments to pass to the method</param>
    /// <returns>String result from the worker method</returns>
    /// <exception cref="InvalidOperationException">Thrown if InitializeAsync was not called</exception>
    /// <exception cref="JSException">Thrown if the worker method throws an exception</exception>
    /// <exception cref="TimeoutException">Thrown if the worker method exceeds the default timeout</exception>
    public static Task<string> InvokeStringAsync(string method, params object[] args)
    {
        return InvokeStringAsync(method, DefaultTimeout, args);
    }

    /// <summary>
    /// Invokes a method on the worker that returns a string.
    /// </summary>
    /// <param name="method">Full method path: "Namespace.ClassName.MethodName"</param>
    /// <param name="timeout">Maximum time to wait for the worker to complete. Use <see cref="Timeout.InfiniteTimeSpan"/> to disable.</param>
    /// <param name="args">Arguments to pass to the method</param>
    /// <returns>String result from the worker method</returns>
    /// <exception cref="InvalidOperationException">Thrown if InitializeAsync was not called</exception>
    /// <exception cref="JSException">Thrown if the worker method throws an exception</exception>
    /// <exception cref="TimeoutException">Thrown if the worker method exceeds the specified timeout</exception>
    public static async Task<string> InvokeStringAsync(string method, TimeSpan timeout, params object[] args)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("WorkerClient is not initialized. Call InitializeAsync first.");
        }

        Task<string> workerTask = InvokeStringInternal(method, args);

        if (timeout == Timeout.InfiniteTimeSpan)
        {
            return await workerTask;
        }

        await Task.WhenAny(workerTask, Task.Delay(timeout));
        if (!workerTask.IsCompleted)
        {
            throw new TimeoutException($"Worker method '{method}' did not complete within {timeout.TotalSeconds:F1} seconds.");
        }

        return await workerTask;
    }
}

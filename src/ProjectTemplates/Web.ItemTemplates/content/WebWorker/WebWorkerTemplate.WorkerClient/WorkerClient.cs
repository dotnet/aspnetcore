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
    private static bool _initialized;

    [JSImport("createWorker", nameof(WorkerClient))]
    private static partial void CreateWorkerInternal();

    [JSImport("waitForReady", nameof(WorkerClient))]
    private static partial Task WaitForReadyInternal();

    [JSImport("invokeString", nameof(WorkerClient))]
    private static partial Task<string> InvokeStringInternal(string method, [JSMarshalAs<JSType.Array<JSType.Any>>] object[] args);

    [JSImport("terminate", nameof(WorkerClient))]
    private static partial void TerminateInternal();

    private static void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("WorkerClient is not initialized. Call InitializeAsync first.");
        }
    }

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

    /// <summary>
    /// Waits for the worker to be fully initialized and ready.
    /// </summary>
    /// <returns>A task that completes when the worker is ready</returns>
    /// <exception cref="InvalidOperationException">Thrown if InitializeAsync was not called</exception>
    public static async Task WaitForReadyAsync()
    {
        EnsureInitialized();
        await WaitForReadyInternal();
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
        EnsureInitialized();
        var workerTask = InvokeStringInternal(method, args);
        return timeout == Timeout.InfiniteTimeSpan
            ? await workerTask
            : await workerTask.WaitAsync(timeout);
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
        EnsureInitialized();
        TerminateInternal();
    }
}

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

        _initialized = true;
    }

    [JSImport("setProgressCallback", nameof(WorkerClient))]
    private static partial void SetProgressCallbackInternal([JSMarshalAs<JSType.Function<JSType.String, JSType.Number, JSType.Number>>] Action<string, int, int> callback);

    /// <summary>
    /// Sets a callback to receive progress updates from worker operations.
    /// </summary>
    /// <param name="callback">Callback function receiving (message, current, total)</param>
    public static void SetProgressCallback(Action<string, int, int>? callback)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("WorkerClient is not initialized. Call InitializeAsync first.");
        }

        if (callback == null)
        {
            SetProgressCallbackInternal((_, _, _) => { }); // Clear callback
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
    /// The next InvokeJsonAsync call will use the new worker instance.
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
    /// Invokes a method on the worker that returns a JSON string and deserializes it to the specified type.
    /// Uses <see cref="DefaultTimeout"/> for timeout.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON result to</typeparam>
    /// <param name="method">Full method path: "Namespace.ClassName.MethodName"</param>
    /// <param name="args">Arguments to pass to the method</param>
    /// <returns>Deserialized result of type T</returns>
    /// <exception cref="InvalidOperationException">Thrown if InitializeAsync was not called</exception>
    /// <exception cref="JSException">Thrown if the worker method throws an exception</exception>
    /// <exception cref="TimeoutException">Thrown if the worker method exceeds the default timeout</exception>
    /// <exception cref="JsonException">Thrown if JSON deserialization fails</exception>
    public static Task<T?> InvokeJsonAsync<T>(string method, params object[] args)
    {
        return InvokeJsonAsync<T>(method, DefaultTimeout, args);
    }

    /// <summary>
    /// Invokes a method on the worker that returns a JSON string and deserializes it to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON result to</typeparam>
    /// <param name="method">Full method path: "Namespace.ClassName.MethodName"</param>
    /// <param name="timeout">Maximum time to wait for the worker to complete. Use <see cref="Timeout.InfiniteTimeSpan"/> to disable.</param>
    /// <param name="args">Arguments to pass to the method</param>
    /// <returns>Deserialized result of type T</returns>
    /// <exception cref="InvalidOperationException">Thrown if InitializeAsync was not called</exception>
    /// <exception cref="JSException">Thrown if the worker method throws an exception</exception>
    /// <exception cref="TimeoutException">Thrown if the worker method exceeds the specified timeout</exception>
    /// <exception cref="JsonException">Thrown if JSON deserialization fails</exception>
    public static async Task<T?> InvokeJsonAsync<T>(string method, TimeSpan timeout, params object[] args)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("WorkerClient is not initialized. Call InitializeAsync first.");
        }

        Task<string> workerTask = InvokeStringInternal(method, args);

        string jsonString;
        if (timeout == Timeout.InfiniteTimeSpan)
        {
            jsonString = await workerTask;
        }
        else
        {
            Task completedTask = await Task.WhenAny(workerTask, Task.Delay(timeout));
            if (completedTask != workerTask)
            {
                throw new TimeoutException($"Worker method '{method}' did not complete within {timeout.TotalSeconds:F1} seconds.");
            }
            jsonString = await workerTask;
        }

        return System.Text.Json.JsonSerializer.Deserialize<T>(jsonString, JsonSerializerOptions);
    }

    private static readonly System.Text.Json.JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

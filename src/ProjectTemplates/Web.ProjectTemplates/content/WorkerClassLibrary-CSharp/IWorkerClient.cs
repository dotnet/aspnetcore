// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Company.WorkerClassLibrary1;

/// <summary>
/// Client for communicating with a WebWorker running .NET code.
/// </summary>
public interface IWorkerClient
{
    /// <summary>
    /// Initializes the worker client. Must be called before invoking any worker methods.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Waits for the worker to be fully initialized and ready.
    /// </summary>
    Task WaitForReadyAsync();

    /// <summary>
    /// Invokes a method on the worker that returns a string.
    /// </summary>
    /// <param name="method">Full method path: "Namespace.ClassName.MethodName"</param>
    /// <param name="timeout">Maximum time to wait for the worker to complete.</param>
    /// <param name="args">Arguments to pass to the method</param>
    Task<string> InvokeStringAsync(string method, TimeSpan timeout, params object[] args);

    /// <summary>
    /// Terminates the current worker and creates a new one.
    /// </summary>
    void Terminate();
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Company.WorkerClassLibrary1;

/// <summary>
/// Client for communicating with a WebWorker running .NET code.
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
/// Example usage in a Blazor component:
/// <code>
/// @inject IWorkerClient Worker
/// 
/// protected override async Task OnAfterRenderAsync(bool firstRender)
/// {
///     if (firstRender)
///     {
///         await Worker.InitializeAsync();
///         await Worker.WaitForReadyAsync();
///     }
/// }
/// 
/// async Task CallWorker()
/// {
///     var result = await Worker.InvokeStringAsync(
///         "MyApp.MyWorker.Process", TimeSpan.FromSeconds(30), "Hello");
/// }
/// </code>
/// </para>
/// </remarks>
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
    /// <param name="timeout">Maximum time to wait. Use <see cref="Timeout.InfiniteTimeSpan"/> to disable.</param>
    /// <param name="args">Arguments to pass to the method.</param>
    /// <returns>The string result from the worker method.</returns>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="InitializeAsync"/> was not called.</exception>
    /// <exception cref="TimeoutException">Thrown if the operation exceeds <paramref name="timeout"/>.</exception>
    /// <exception cref="JSException">Thrown if the worker method throws an exception.</exception>
    Task<string> InvokeStringAsync(string method, TimeSpan timeout, params object[] args);

    /// <summary>
    /// Terminates the current worker and creates a new one.
    /// </summary>
    void Terminate();
}

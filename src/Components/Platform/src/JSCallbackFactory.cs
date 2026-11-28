// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Internal;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Creates JavaScript functions that call a <see cref="JSCallback{T1}"/> through the JavaScript module of this package.
/// </summary>
internal sealed class JSCallbackFactory : IInternalAsyncDisposal
{
    private const string ModulePath = "./_content/Microsoft.AspNetCore.Components.Platform/Microsoft.AspNetCore.Components.Platform.js";

    private readonly IJSRuntime _jsRuntime;
    private Task<IJSObjectReference>? _moduleTask;
    private bool _disposed;

    public JSCallbackFactory(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Creates a JavaScript function that calls <paramref name="callback"/>.
    /// </summary>
    /// <param name="callback">The <see cref="JSCallback{T1}"/> to call.</param>
    /// <param name="shapesJson">
    /// A JSON array with one <c>ArgumentShape</c> (see <c>js/Platform.ts</c>) per parameter of <paramref name="callback"/>.
    /// </param>
    /// <returns>
    /// The function, and the reference that lets JavaScript reach <paramref name="callback"/>.
    /// The caller owns both and releases them when the function is no longer used.
    /// </returns>
    public async Task<(IJSObjectReference Function, IDisposable CallbackReference)> CreateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TCallback>(
        TCallback callback,
        string shapesJson)
        where TCallback : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var module = await GetModuleAsync();
        var callbackReference = DotNetObjectReference.Create(callback);

        try
        {
            var function = await module.InvokeAsync<IJSObjectReference>("createCallback", callbackReference, shapesJson);
            return (function, callbackReference);
        }
        catch
        {
            callbackReference.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Runs a browser operation that calls <paramref name="callback"/> at most once and waits for the <see cref="Task"/> it returns,
    /// such as <c>navigator.locks.request()</c>. The JavaScript function is released when the operation settles.
    /// </summary>
    /// <param name="callback">The .NET callback that the operation calls.</param>
    /// <param name="shapesJson">
    /// A JSON array with one <c>ArgumentShape</c> (see <c>js/Platform.ts</c>) for the argument of <paramref name="callback"/>.
    /// </param>
    /// <param name="operation">The operation, which receives the JavaScript function that calls <paramref name="callback"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when the operation settles.</returns>
    /// <remarks>
    /// An exception from <paramref name="callback"/> rejects the promise that the browser waits for, and is thrown from this method
    /// in place of the <see cref="JSException"/> that the rejected operation produces.
    /// </remarks>
    public async Task InvokeWithCallbackAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] T1>(
        Func<T1, Task> callback,
        string shapesJson,
        Func<IJSObjectReference, Task> operation)
    {
        ExceptionDispatchInfo? callbackException = null;
        var jsCallback = new JSCallback<T1>
        {
            Handler = async arg =>
            {
                try
                {
                    await callback(arg);
                }
                catch (Exception ex)
                {
                    // Rethrown so that the browser sees the failure, such as to release a lock.
                    callbackException = ExceptionDispatchInfo.Capture(ex);
                    throw;
                }
            },
        };

        var (function, callbackReference) = await CreateAsync(jsCallback, shapesJson);

        try
        {
            await operation(function);
        }
        catch (JSException)
        {
            // Not an exception filter: WebAssembly skipped this clause with "when", and the JSException reached the caller.
            callbackException?.Throw();
            throw;
        }
        finally
        {
            try
            {
                await function.DisposeAsync();
            }
            finally
            {
                callbackReference.Dispose();
            }
        }
    }

    async ValueTask IInternalAsyncDisposal.InternalDisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Functions created from the module keep working after the module reference is released.
        if (_moduleTask is { IsCompletedSuccessfully: true } moduleTask)
        {
            await moduleTask.Result.DisposeAsync();
        }
    }

    private Task<IJSObjectReference> GetModuleAsync()
    {
        if (_moduleTask is null || _moduleTask.IsFaulted || _moduleTask.IsCanceled)
        {
            _moduleTask = ImportModuleAsync();
        }

        return _moduleTask;
    }

    private async Task<IJSObjectReference> ImportModuleAsync()
    {
        var module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);

        // Disposal does not wait for the import, so nothing else releases a module that arrives after it.
        if (_disposed)
        {
            await module.DisposeAsync();
            throw new ObjectDisposedException(GetType().Name);
        }

        return module;
    }
}

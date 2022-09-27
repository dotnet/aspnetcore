// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// A mechanism for queuing JS-to-.NET calls so they aren't nested on the call stack and hence
// have the same ordering behaviors as in Blazor Server. This eliminates serveral inconsistency
// problems and bugs that otherwise require special-case solutions in other parts of the code.
//
// The reason for not using an actual SynchronizationContext for this is that, historically,
// Blazor WebAssembly has not enforced any rule around having to dispatch to a sync context.
// Adding such a rule now would be too breaking, given how component libraries may be reliant
// on being able to render at any time without InvokeAsync. If we add true multithreading in the
// future, we should start enforcing dispatch if (and only if) multithreading is enabled.
// For now, this minimal work queue is an internal detail of how the framework dispatches
// incoming JS->.NET calls and makes sure they get deferred if a renderbatch is in process.

internal static class WebAssemblyCallQueue
{
    private static bool _isCallInProgress;
    private static readonly Queue<Action> _pendingWork = new();

    public static bool IsInProgress => _isCallInProgress;
    public static bool HasUnstartedWork => _pendingWork.Count > 0;

    /// <summary>
    /// Runs the supplied callback when possible. If the call queue is empty, the callback is executed
    /// synchronously. If some call is already executing within the queue, the callback is added to the
    /// back of the queue and will be executed in turn.
    /// </summary>
    /// <typeparam name="T">The type of a state parameter for the callback</typeparam>
    /// <param name="state">A state parameter for the callback. If the callback is able to execute synchronously, this allows us to avoid any allocations for the closure.</param>
    /// <param name="callback">The callback to run.</param>
    /// <remarks>
    /// In most cases this should only be used for callbacks that will not throw, because
    /// [1] Unhandled exceptions will be fatal to the application, as the work queue will no longer process
    ///     further items (just like unhandled hub exceptions in Blazor Server)
    /// [2] The exception will be thrown at the point of the top-level caller, which is not necessarily the
    ///     code that scheduled the callback, so you may not be able to observe it.
    ///
    /// We could change this to return a Task and do the necessary try/catch things to direct exceptions back
    /// to the code that scheduled the callback, but it's not required for current use cases and would require
    /// at least an extra allocation and layer of try/catch per call, plus more work to schedule continuations
    /// at the call site.
    /// </remarks>
    public static void Schedule<T>(T state, Action<T> callback)
    {
        if (_isCallInProgress)
        {
            _pendingWork.Enqueue(() => callback(state));
        }
        else
        {
            _isCallInProgress = true;
            callback(state);

            // Now run any queued work items
            while (_pendingWork.TryDequeue(out var nextWorkItem))
            {
                nextWorkItem();
            }

            _isCallInProgress = false;
        }
    }
}

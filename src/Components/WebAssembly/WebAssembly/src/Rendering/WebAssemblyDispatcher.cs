// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering;

// When Blazor is deployed with multi-threaded runtime, WebAssemblyDispatcher will help to dispatch all Blazor JS interop calls to the main thread.
// This is necessary because all JS objects have thread affinity. They are only available on the thread (WebWorker) which created them.
// Also DOM is only available on the main (browser) thread.
// Because all of the Dispatcher.InvokeAsync methods return Task, we don't need to propagate errors via OnUnhandledException handler
internal sealed class WebAssemblyDispatcher : Dispatcher
{
    internal static SynchronizationContext? _mainSynchronizationContext;
    internal static int _mainManagedThreadId;

    // we really need the UI thread not just the right context, because JS objects have thread affinity
    public override bool CheckAccess() => _mainManagedThreadId == Environment.CurrentManagedThreadId;

    public override Task InvokeAsync(Action workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        if (CheckAccess())
        {
            // this branch executes on correct thread and solved JavaScript objects thread affinity
            // but it executes out of order, if there are some pending jobs in the _mainSyncContext already, same as RendererSynchronizationContextDispatcher
            workItem();
            // it can throw synchronously, same as RendererSynchronizationContextDispatcher
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource();

        // RendererSynchronizationContext doesn't need to deal with thread affinity and so it could execute jobs on calling thread as optimization.
        // we could not do it for WASM/JavaScript, because we need to solve for thread affinity of JavaScript objects, so we always Post into the queue.
        _mainSynchronizationContext!.Post(static (object? o) =>
        {
            var state = ((TaskCompletionSource tcs, Action workItem))o!;
            try
            {
                state.workItem();
                state.tcs.SetResult();
            }
            catch (Exception ex)
            {
                state.tcs.SetException(ex);
            }
        }, (tcs, workItem));

        return tcs.Task;
    }

    public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        if (CheckAccess())
        {
            // it can throw synchronously, same as RendererSynchronizationContextDispatcher
            return Task.FromResult(workItem());
        }

        var tcs = new TaskCompletionSource<TResult>();

        _mainSynchronizationContext!.Post(static (object? o) =>
        {
            var state = ((TaskCompletionSource<TResult> tcs, Func<TResult> workItem))o!;
            try
            {
                var res = state.workItem();
                state.tcs.SetResult(res);
            }
            catch (Exception ex)
            {
                state.tcs.SetException(ex);
            }
        }, (tcs, workItem));

        return tcs.Task;
    }

    public override Task InvokeAsync(Func<Task> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        if (CheckAccess())
        {
            // this branch executes on correct thread and solved JavaScript objects thread affinity
            // but it executes out of order, if there are some pending jobs in the _mainSyncContext already, same as RendererSynchronizationContextDispatcher
            return workItem();
            // it can throw synchronously, same as RendererSynchronizationContextDispatcher
        }

        var tcs = new TaskCompletionSource();

        _mainSynchronizationContext!.Post(static (object? o) =>
        {
            var state = ((TaskCompletionSource tcs, Func<Task> workItem))o!;

            try
            {
                state.workItem().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        state.tcs.SetException(t.Exception);
                    }
                    else if (t.IsCanceled)
                    {
                        state.tcs.SetCanceled();
                    }
                    else
                    {
                        state.tcs.SetResult();
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                // it could happen that the workItem will throw synchronously
                state.tcs.SetException(ex);
            }
        }, (tcs, workItem));

        return tcs.Task;
    }

    public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        if (CheckAccess())
        {
            // this branch executes on correct thread and solved JavaScript objects thread affinity
            // but it executes out of order, if there are some pending jobs in the _mainSyncContext already, same as RendererSynchronizationContextDispatcher
            return workItem();
            // it can throw synchronously, same as RendererSynchronizationContextDispatcher
        }

        var tcs = new TaskCompletionSource<TResult>();

        _mainSynchronizationContext!.Post(static (object? o) =>
        {
            var state = ((TaskCompletionSource<TResult> tcs, Func<Task<TResult>> workItem))o!;
            try
            {
                state.workItem().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        state.tcs.SetException(t.Exception);
                    }
                    else if (t.IsCanceled)
                    {
                        state.tcs.SetCanceled();
                    }
                    else
                    {
                        state.tcs.SetResult(t.Result);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                state.tcs.SetException(ex);
            }
        }, (tcs, workItem));

        return tcs.Task;
    }
}

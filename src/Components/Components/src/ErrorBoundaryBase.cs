// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// A base class for error boundary components.
/// </summary>
public abstract class ErrorBoundaryBase : ComponentBase, IErrorBoundary
{
    private int _errorCount;

    /// <summary>
    /// The content to be displayed when there is no error.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The content to be displayed when there is an error.
    /// </summary>
    [Parameter] public RenderFragment<Exception>? ErrorContent { get; set; }

    /// <summary>
    /// The maximum number of errors that can be handled. If more errors are received,
    /// they will be treated as fatal. Calling <see cref="Recover"/> resets the count.
    /// </summary>
    [Parameter] public int MaximumErrorCount { get; set; } = 100;

    /// <summary>
    /// Gets the current exception, or null if there is no exception.
    /// </summary>
    protected Exception? CurrentException { get; private set; }

    /// <summary>
    /// Resets the error boundary to a non-errored state. If the error boundary is not
    /// already in an errored state, the call has no effect.
    /// </summary>
    public void Recover()
    {
        if (CurrentException is not null)
        {
            _errorCount = 0;
            CurrentException = null;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Invoked by the base class when an error is being handled. Typically, derived classes
    /// should log the exception from this method.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> being handled.</param>
    protected abstract Task OnErrorAsync(Exception exception);

    void IErrorBoundary.HandleException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // If rendering the error content itself causes an error, then re-rendering on error risks creating an
        // infinite error loop. Unfortunately it's very hard to distinguish whether the error source is "child content"
        // or "error content", since the exceptions can be received asynchronously, arbitrarily long after we switched
        // between normal and errored states. Without creating a very intricate coupling between ErrorBoundaryBase and
        // Renderer internals, the obvious options are either:
        //
        // [a] Don't re-render if we're already in an error state. This is problematic because the renderer needs to
        //     discard the error boundary's subtree on every error, in case a custom error boundary fails to do so, and
        //     hence we'd be left with a blank UI if we didn't re-render.
        // [b] Do re-render each time, and trust the developer not to cause errors from their error content.
        //
        // As a middle ground, we try to detect excessive numbers of errors arriving in between recoveries, and treat
        // an excess as fatal. This also helps to expose the case where a child continues to throw (e.g., on a timer),
        // which would be very inefficient.
        if (++_errorCount > MaximumErrorCount)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        // Notify the subclass so it can begin any async operation even before we render, because (for example)
        // we want logs to be written before rendering in case the rendering throws. But there's no reason to
        // wait for the async operation to complete before we render.
        var onErrorTask = OnErrorAsync(exception);
        if (!onErrorTask.IsCompletedSuccessfully)
        {
            _ = HandleOnErrorExceptions(onErrorTask);
        }

        CurrentException = exception;
        StateHasChanged();
    }

    private async Task HandleOnErrorExceptions(Task onExceptionTask)
    {
        if (onExceptionTask.IsFaulted)
        {
            // Synchronous error handling exceptions can simply be fatal to the circuit
            ExceptionDispatchInfo.Capture(onExceptionTask.Exception!).Throw();
        }
        else
        {
            // Async exceptions are tricky because there's no natural way to bring them back
            // onto the sync context within their original circuit. The closest approximation
            // we have is trying to rethrow via rendering. If, in the future, we add an API for
            // directly dispatching an exception from ComponentBase, we should use that here.
            try
            {
                await onExceptionTask;
            }
            catch (Exception exception)
            {
                CurrentException = exception;
                ChildContent = _ => ExceptionDispatchInfo.Capture(exception).Throw();
                ErrorContent = _ => _ => ExceptionDispatchInfo.Capture(exception).Throw();
                StateHasChanged();
            }
        }
    }
}

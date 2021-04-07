// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// A base class for error boundary components.
    /// </summary>
    public abstract class ErrorBoundaryBase : ComponentBase, IErrorBoundary
    {
        /// <summary>
        /// The content to be displayed when there is no error.
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// The content to be displayed when there is an error.
        /// </summary>
        [Parameter] public RenderFragment<Exception>? ErrorContent { get; set; }

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
                CurrentException = null;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Invoked by the base class when an error is being handled.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> being handled.</param>
        protected virtual Task OnErrorAsync(Exception exception)
        {
            return Task.CompletedTask;
        }

        void IErrorBoundary.HandleException(Exception exception)
        {
            var onErrorTask = OnErrorAsync(exception);
            if (!onErrorTask.IsCompletedSuccessfully)
            {
                _ = HandleOnErrorExceptions(onErrorTask);
            }

            // If there's an error while we're already displaying error content, then it may be either of:
            //  (a) the error content that is failing
            //  (b) some earlier child content failing an asynchronous task that began before we entered an error state
            // In case it's (a), we don't want to risk triggering an infinite error rendering loop by re-rendering.
            // This isn't harmful in case (b) because we're already in an error state, so don't need to update the UI further.
            // So, only re-render if we're not currently in an error state.
            if (CurrentException == null)
            {
                CurrentException = exception;
                StateHasChanged();
            }
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
}

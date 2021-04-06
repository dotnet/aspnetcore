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
        /// Specifies whether to reset the error state each time this component instance is rendered
        /// by its parent. This allows the child content to be recreated in an attempt to recover from the error.
        /// </summary>
        [Parameter] public bool AutoRecover { get; set; }

        /// <summary>
        /// Gets the current exception, or null if there is no exception.
        /// </summary>
        protected Exception? CurrentException { get; private set; }

        /// <inheritdoc />
        public override Task SetParametersAsync(ParameterView parameters)
        {
            if (AutoRecover)
            {
                CurrentException = null;
            }

            return base.SetParametersAsync(parameters);
        }

        /// <summary>
        /// Invoked by the base class when an error is being handled.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> being handled.</param>
        protected abstract Task OnErrorAsync(Exception exception);

        void IErrorBoundary.HandleException(Exception exception)
        {
            if (CurrentException is not null)
            {
                // If there's an error while we're already displaying error content, then it's the
                // error content that's failing. Avoid the risk of an infinite error rendering loop.
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

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
}

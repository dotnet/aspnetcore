// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// A base class for error boundary components.
    /// </summary>
    public abstract class ErrorBoundaryBase : IComponent, IErrorBoundary
    {
        // This deliberately doesn't inherit from ComponentBase because it's not intended to be
        // subclassable using a .razor file. ErrorBoundaryBase shouldn't be used as a base class
        // for all components indiscriminately, because that will lead to undesirable error-ignoring
        // patterns. We expect that subclassing ErrorBoundaryBase to be done mainly by platform
        // authors, providing just a default error UI for their rendering technology and not
        // customizing other aspects of the semantics, such as whether to re-render after an error.

        private RenderHandle _renderHandle;
        private Exception? _currentException;

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
        [Parameter] public bool AutoReset { get; set; }

        /// <inheritdoc />
        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        /// <inheritdoc />
        public Task SetParametersAsync(ParameterView parameters)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.Name.Equals(nameof(ChildContent), StringComparison.OrdinalIgnoreCase))
                {
                    ChildContent = (RenderFragment)parameter.Value;
                }
                else if (parameter.Name.Equals(nameof(ErrorContent), StringComparison.OrdinalIgnoreCase))
                {
                    ErrorContent = (RenderFragment<Exception>)parameter.Value;
                }
                else if (parameter.Name.Equals(nameof(AutoReset), StringComparison.OrdinalIgnoreCase))
                {
                    AutoReset = (bool)parameter.Value;
                }
                else
                {
                    throw new ArgumentException($"The component '{GetType().FullName}' does not accept a parameter with the name '{parameter.Name}'.");
                }
            }

            if (AutoReset)
            {
                _currentException = null;
            }

            _renderHandle.Render(Render);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs the exception.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> being handled.</param>
        protected abstract ValueTask LogExceptionAsync(Exception exception);

        /// <summary>
        /// Renders the default error content. This will only be used when <see cref="ErrorContent"/>
        /// was not supplied.
        /// </summary>
        /// <param name="builder">The <see cref="RenderTreeBuilder"/></param>
        /// <param name="exception">The current exception.</param>
        protected abstract void RenderDefaultErrorContent(RenderTreeBuilder builder, Exception exception);

        void IErrorBoundary.HandleException(Exception exception)
        {
            if (_currentException is not null)
            {
                // If there's an error while we're already displaying error content, then it's the
                // error content that's failing. Avoid the risk of an infinite error rendering loop.
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            var logExceptionTask = LogExceptionAsync(exception);
            _currentException = exception;
            _renderHandle.Render(Render);

            // If the logger is failing, show its exception in preference to showing the original
            // exception, since otherwise the developer has no way to discover it.
            if (!logExceptionTask.IsCompletedSuccessfully)
            {
                _ = HandleAnyLoggingErrors(logExceptionTask);
            }
        }

        private async Task HandleAnyLoggingErrors(ValueTask logExceptionTask)
        {
            try
            {
                await logExceptionTask;
            }
            catch (Exception exception)
            {
                _currentException = exception;
                _renderHandle.Render(Render);
            }
        }

        private void Render(RenderTreeBuilder builder)
        {
            if (_currentException is null)
            {
                builder.AddContent(0, ChildContent);
            }
            else if (ErrorContent is not null)
            {
                builder.AddContent(1, ErrorContent(_currentException));
            }
            else
            {
                // Even if the subclass tries to re-render the same ChildContent as its default error content,
                // we still won't reuse the subtree components because they are in a different region. So we
                // can be sure the old tree will be correctly disposed.
                builder.OpenRegion(2);
                RenderDefaultErrorContent(builder, _currentException);
                builder.CloseRegion();
            }
        }
    }
}

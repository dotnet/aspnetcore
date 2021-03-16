// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Web
{
    // TODO: Reimplement directly on IComponent
    public sealed class ErrorBoundary : ComponentBase, IErrorBoundary
    {
        private Exception? _currentException;

        [Inject] private IErrorBoundaryLogger? ErrorBoundaryLogger { get; set; }

        /// <summary>
        /// Specifies whether to reset the error state each time the <see cref="ErrorBoundary"/> is rendered
        /// by its parent. This allows the child content to be recreated in an attempt to recover from the error.
        /// </summary>
        [Parameter] public bool AutoReset { get; set; }

        [Parameter] public RenderFragment? ChildContent { get; set; }

        [Parameter] public RenderFragment<Exception>? ErrorContent { get; set; }

        public void HandleException(Exception exception)
        {
            if (_currentException is not null)
            {
                // If there's an error while we're already displaying error content, then it's the
                // error content that's failing. Avoid the risk of an infinite error rendering loop.
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            _ = ErrorBoundaryLogger!.LogErrorAsync(exception, clientOnly: false);

            _currentException = exception;
            StateHasChanged();
        }

        protected override void OnParametersSet()
        {
            // Not totally sure about this, but here we auto-recover if the parent component
            // re-renders us. This has the benefit that in your layout, you can just wrap this
            // around @Body and it does what you expect (recovering on navigate). But it might
            // make other cases more awkward because the parent will keep recreating any children
            // that just error out on init.
            if (AutoReset)
            {
                _currentException = null;
            }
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var exception = _currentException;
            if (exception is null)
            {
                builder.AddContent(0, ChildContent);
            }
            else if (ErrorContent is not null)
            {
                builder.AddContent(1, ErrorContent(exception));
            }
            else
            {
                builder.OpenRegion(2);
                // TODO: Better UI (some kind of language-independent icon, CSS stylability)
                // Could it simply be <div class="error-boundary-error"></div>, with all the
                // content provided in template CSS? Is this even going to be used in the
                // template by default? It's probably best have some default UI that doesn't
                // rely on there being any CSS, but make it overridable via CSS.
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "onclick", (Func<MouseEventArgs, ValueTask>)(_ =>
                    // Re-log, to help the developer figure out which ErrorBoundary issued which log message
                    ErrorBoundaryLogger!.LogErrorAsync(exception, clientOnly: true)));
                builder.AddContent(1, "Error");
                builder.CloseElement();
                builder.CloseRegion();
            }
        }
    }
}

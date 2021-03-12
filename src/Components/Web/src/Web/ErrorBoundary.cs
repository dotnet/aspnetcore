// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web
{
    // TODO: Reimplement directly on IComponent
    public sealed class ErrorBoundary : ComponentBase, IErrorBoundary
    {
        private FormattedError? _currentError;

        [Inject] private IJSRuntime? JS { get; set; }

        [Parameter] public RenderFragment? ChildContent { get; set; }

        // Notice that, to respect the IClientErrorFormatter, we have to do this in terms of a Message/Details
        // pair, and not in terms of the original Exception. We can't assume that developers who provide an
        // ErrorContent understand the issues with exposing the raw exception to the client.
        [Parameter] public RenderFragment<FormattedError>? ErrorContent { get; set; }

        // TODO: Eliminate the enableDetailedErrors flag (and corresponding API on Renderer)
        // and instead have some new default DI service, IClientErrorFormatter.
        public void HandleException(Exception exception)
        {
            // TODO: We should log the underlying exception to some ILogger using the same
            // severity that we did before for unhandled exceptions
            _currentError = FormatErrorForClient(exception);
            StateHasChanged();

            // TODO: Should there be an option not to auto-log exceptions to the console?
            // Probably not. Don't want people thinking it's fine to have such exceptions.
            _ = LogExceptionToClientIfPossible();
        }

        protected override void OnParametersSet()
        {
            // Not totally sure about this, but here we auto-recover if the parent component
            // re-renders us. This has the benefit that in your layout, you can just wrap this
            // around @Body and it does what you expect (recovering on navigate). But it might
            // make other cases more awkward because the parent will keep recreating any children
            // that just error out on init. Maybe it should be an option.
            _currentError = null;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (_currentError is null)
            {
                builder.AddContent(0, ChildContent);
            }
            else if (ErrorContent is not null)
            {
                builder.AddContent(1, ErrorContent(_currentError));
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
                {
                    return LogExceptionToClientIfPossible();
                }));

                builder.AddContent(1, "Error");
                builder.CloseElement();
                builder.CloseRegion();
            }
        }

        private async ValueTask LogExceptionToClientIfPossible()
        {
            // TODO: Handle prerendering too. We can't use IJSRuntime while prerendering.
            if (_currentError is not null)
            {
                await JS!.InvokeVoidAsync("console.error", $"{_currentError.Message}\n{_currentError.Details}");
            }
        }

        // TODO: Move into a new IClientErrorFormatter service
        private static FormattedError FormatErrorForClient(Exception exception)
        {
            // TODO: Obviously this will be internal to IClientErrorFormatter
            var enableDetailedErrors = true;

            return enableDetailedErrors
                ? new FormattedError(exception.Message, exception.StackTrace ?? string.Empty)
                : new FormattedError("There was an error", "For more details turn on detailed exceptions by setting 'DetailedErrors: true' in 'appSettings.Development.json' or set 'CircuitOptions.DetailedErrors'.");
        }

        public record FormattedError
        {
            public FormattedError(string message, string details)
            {
                Message = message;
                Details = details;
            }

            public string Message { get; }
            public string Details { get; }
        }
    }
}

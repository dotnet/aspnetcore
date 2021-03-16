// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// Captures errors thrown from its child content.
    /// </summary>
    public sealed class ErrorBoundary : ErrorBoundaryBase
    {
        [Inject] private IErrorBoundaryLogger? ErrorBoundaryLogger { get; set; }

        /// <inheritdoc />
        protected override ValueTask LogExceptionAsync(Exception exception)
        {
            return ErrorBoundaryLogger!.LogErrorAsync(exception, clientOnly: false);
        }

        /// <inheritdoc />
        protected override void RenderDefaultErrorContent(RenderTreeBuilder builder, Exception exception)
        {
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
        }
    }
}

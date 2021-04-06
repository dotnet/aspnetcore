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
    public class ErrorBoundary : ErrorBoundaryBase
    {
        [Inject] private IErrorBoundaryLogger? ErrorBoundaryLogger { get; set; }

        /// <inheritdoc />
        protected override ValueTask LogExceptionAsync(Exception exception)
        {
            return ErrorBoundaryLogger!.LogErrorAsync(exception);
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (CurrentException is null)
            {
                builder.AddContent(0, ChildContent);
            }
            else if (ErrorContent is not null)
            {
                builder.AddContent(1, ErrorContent(CurrentException));
            }
            else
            {
                // The default error UI doesn't include any content, because:
                // [1] We don't know whether or not you'd be happy to show the stack trace. It depends both on
                //     whether DetailedErrors is enabled and whether you're in production, because even on WebAssembly
                //     you likely don't want to put technical data like that in the UI for end users. A reasonable way
                //     to toggle this is via something like "#if DEBUG" but that can only be done in user code.
                // [2] We can't have any other human-readable content by default, because it would need to be valid
                //     for all languages.
                // Instead, the default project template provides locale-specific default content via CSS. This provides
                // a quick form of customization even without having to subclass this component.
                builder.OpenElement(2, "div");
                builder.AddAttribute(3, "class", "blazor-error-boundary");
                builder.CloseElement();
            }
        }
    }
}

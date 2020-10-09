// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    /// <summary>
    /// Selects a handler method from a page.
    /// </summary>
    public interface IPageHandlerMethodSelector
    {
        /// <summary>
        /// Selects a handler method from a page.
        /// </summary>
        /// <param name="context">The <see cref="PageContext"/>.</param>
        /// <returns>The selected <see cref="HandlerMethodDescriptor"/>.</returns>
        HandlerMethodDescriptor Select(PageContext context);
    }
}

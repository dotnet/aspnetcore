// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A filter that surrounds execution of a page handler method.
    /// </summary>
    public interface IPageFilter : IFilterMetadata
    {
        /// <summary>
        /// Called after a handler method has been selected, but before model binding occurs.
        /// </summary>
        /// <param name="context">The <see cref="PageHandlerSelectedContext"/>.</param>
        void OnPageHandlerSelected(PageHandlerSelectedContext context);

        /// <summary>
        /// Called before the handler method executes, after model binding is complete.
        /// </summary>
        /// <param name="context">The <see cref="PageHandlerExecutingContext"/>.</param>
        void OnPageHandlerExecuting(PageHandlerExecutingContext context);

        /// <summary>
        /// Called after the handler method executes, before the action method is invoked.
        /// </summary>
        /// <param name="context">The <see cref="PageHandlerExecutedContext"/>.</param>
        void OnPageHandlerExecuted(PageHandlerExecutedContext context);
    }
}

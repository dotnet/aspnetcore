// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A filter that surrounds execution of the action result.
    /// </summary>
    public interface IResultFilter : IFilterMetadata
    {
        /// <summary>
        /// Called before the action result executes.
        /// </summary>
        /// <param name="context">The <see cref="ResultExecutingContext"/>.</param>
        void OnResultExecuting(ResultExecutingContext context);

        /// <summary>
        /// Called after the action result executes.
        /// </summary>
        /// <param name="context">The <see cref="ResultExecutedContext"/>.</param>
        void OnResultExecuted(ResultExecutedContext context);
    }
}
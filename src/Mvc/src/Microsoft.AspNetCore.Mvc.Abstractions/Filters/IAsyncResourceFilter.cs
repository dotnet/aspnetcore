// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A filter that asynchronously surrounds execution of model binding, the action (and filters) and the action
    /// result (and filters).
    /// </summary>
    public interface IAsyncResourceFilter : IFilterMetadata
    {
        /// <summary>
        /// Called asynchronously before the rest of the pipeline.
        /// </summary>
        /// <param name="context">The <see cref="ResourceExecutingContext"/>.</param>
        /// <param name="next">
        /// The <see cref="ResourceExecutionDelegate"/>. Invoked to execute the next resource filter or the remainder
        /// of the pipeline.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> which will complete when the remainder of the pipeline completes.
        /// </returns>
        Task OnResourceExecutionAsync(
            ResourceExecutingContext context,
            ResourceExecutionDelegate next);
    }
}
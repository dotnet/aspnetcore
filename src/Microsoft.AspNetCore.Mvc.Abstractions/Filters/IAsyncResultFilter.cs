// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A filter that asynchronously surrounds execution of the action result.
    /// </summary>
    public interface IAsyncResultFilter : IFilterMetadata
    {
        /// <summary>
        /// Called asynchronously before the action result.
        /// </summary>
        /// <param name="context">The <see cref="ResultExecutingContext"/>.</param>
        /// <param name="next">
        /// The <see cref="ResultExecutionDelegate"/>. Invoked to execute the next result filter or the result itself.
        /// </param>
        /// <returns>A <see cref="Task"/> that on completion indicates the filter has executed.</returns>
        Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next);
    }
}

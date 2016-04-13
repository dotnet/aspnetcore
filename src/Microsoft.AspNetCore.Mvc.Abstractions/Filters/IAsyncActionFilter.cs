// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A filter that asynchronously surrounds execution of the action, after model binding is complete.
    /// </summary>
    public interface IAsyncActionFilter : IFilterMetadata
    {
        /// <summary>
        /// Called asynchronously before the action, after model binding is complete.
        /// </summary>
        /// <param name="context">The <see cref="ActionExecutingContext"/>.</param>
        /// <param name="next">
        /// The <see cref="ActionExecutionDelegate"/>. Invoked to execute the next action filter or the action itself.
        /// </param>
        /// <returns>A <see cref="Task"/> that on completion indicates the filter has executed.</returns>
        Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next);
    }
}

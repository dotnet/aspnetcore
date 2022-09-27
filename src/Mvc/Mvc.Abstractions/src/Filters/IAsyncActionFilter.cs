// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A filter that surrounds execution of the action.
/// </summary>
public interface IActionFilter : IFilterMetadata
{
    /// <summary>
    /// Called before the action executes, after model binding is complete.
    /// </summary>
    /// <param name="context">The <see cref="ActionExecutingContext"/>.</param>
    void OnActionExecuting(ActionExecutingContext context);

    /// <summary>
    /// Called after the action executes, before the action result.
    /// </summary>
    /// <param name="context">The <see cref="ActionExecutedContext"/>.</param>
    void OnActionExecuted(ActionExecutedContext context);
}

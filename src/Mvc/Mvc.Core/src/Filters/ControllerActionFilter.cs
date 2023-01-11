// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A filter implementation which delegates to the controller for action filter interfaces.
/// </summary>
internal sealed class ControllerActionFilter : IAsyncActionFilter, IOrderedFilter
{
    // Controller-filter methods run farthest from the action by default.
    /// <inheritdoc />
    public int Order { get; set; } = int.MinValue;

    /// <inheritdoc />
    public Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var controller = context.Controller;
        if (controller == null)
        {
            throw new InvalidOperationException(Resources.FormatPropertyOfTypeCannotBeNull(
                nameof(context.Controller),
                nameof(ActionExecutingContext)));
        }

        if (controller is IAsyncActionFilter asyncActionFilter)
        {
            return asyncActionFilter.OnActionExecutionAsync(context, next);
        }
        else if (controller is IActionFilter actionFilter)
        {
            return ExecuteActionFilter(context, next, actionFilter);
        }
        else
        {
            return next();
        }
    }

    private static async Task ExecuteActionFilter(
        ActionExecutingContext context,
        ActionExecutionDelegate next,
        IActionFilter actionFilter)
    {
        actionFilter.OnActionExecuting(context);
        if (context.Result == null)
        {
            actionFilter.OnActionExecuted(await next());
        }
    }
}

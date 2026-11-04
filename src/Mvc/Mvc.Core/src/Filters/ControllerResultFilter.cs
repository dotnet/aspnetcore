// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A filter implementation which delegates to the controller for result filter interfaces.
/// </summary>
internal sealed class ControllerResultFilter : IAsyncResultFilter, IOrderedFilter
{
    // Controller-filter methods run farthest from the result by default.
    /// <inheritdoc />
    public int Order { get; set; } = int.MinValue;

    /// <inheritdoc />
    public Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var controller = context.Controller;
        if (controller == null)
        {
            throw new InvalidOperationException(Resources.FormatPropertyOfTypeCannotBeNull(
                nameof(context.Controller),
                nameof(ResultExecutingContext)));
        }

        if (controller is IAsyncResultFilter asyncResultFilter)
        {
            return asyncResultFilter.OnResultExecutionAsync(context, next);
        }
        else if (controller is IResultFilter resultFilter)
        {
            return ExecuteResultFilter(context, next, resultFilter);
        }
        else
        {
            return next();
        }
    }

    private static async Task ExecuteResultFilter(
        ResultExecutingContext context,
        ResultExecutionDelegate next,
        IResultFilter resultFilter)
    {
        resultFilter.OnResultExecuting(context);
        if (!context.Cancel)
        {
            resultFilter.OnResultExecuted(await next());
        }
    }
}

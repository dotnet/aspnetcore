// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Filters;

internal sealed class PageHandlerResultFilter : IAsyncResultFilter, IOrderedFilter
{
    /// <remarks>
    /// Filters on handlers run furthest from the action.
    /// </remarks>
    public int Order => int.MinValue;

    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var handler = context.Controller;
        if (handler == null)
        {
            throw new InvalidOperationException(Resources.FormatPropertyOfTypeCannotBeNull(
                nameof(context.Controller),
                nameof(ResultExecutingContext)));
        }

        if (handler is IAsyncResultFilter asyncResultFilter)
        {
            return asyncResultFilter.OnResultExecutionAsync(context, next);
        }
        else if (handler is IResultFilter resultFilter)
        {
            return ExecuteSyncFilter(context, next, resultFilter);
        }
        else
        {
            return next();
        }
    }

    private static async Task ExecuteSyncFilter(
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

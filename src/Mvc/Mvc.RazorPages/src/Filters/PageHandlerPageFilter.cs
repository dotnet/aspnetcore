// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Filters;

internal sealed class PageHandlerPageFilter : IAsyncPageFilter, IOrderedFilter
{
    /// <remarks>
    /// Filters on handlers run furthest from the action.
    /// </remarks>t
    public int Order => int.MinValue;

    public Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var handlerInstance = context.HandlerInstance;
        if (handlerInstance == null)
        {
            throw new InvalidOperationException(Resources.FormatPropertyOfTypeCannotBeNull(
                nameof(context.HandlerInstance),
                nameof(PageHandlerExecutedContext)));
        }

        if (handlerInstance is IAsyncPageFilter asyncPageFilter)
        {
            return asyncPageFilter.OnPageHandlerExecutionAsync(context, next);
        }
        else if (handlerInstance is IPageFilter pageFilter)
        {
            return ExecuteSyncFilter(context, next, pageFilter);
        }
        else
        {
            return next();
        }
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.HandlerInstance is IAsyncPageFilter asyncPageFilter)
        {
            return asyncPageFilter.OnPageHandlerSelectionAsync(context);
        }
        else if (context.HandlerInstance is IPageFilter pageFilter)
        {
            pageFilter.OnPageHandlerSelected(context);
        }

        return Task.CompletedTask;
    }

    private static async Task ExecuteSyncFilter(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next,
        IPageFilter pageFilter)
    {
        pageFilter.OnPageHandlerExecuting(context);
        if (context.Result == null)
        {
            pageFilter.OnPageHandlerExecuted(await next());
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageHandlerPageFilter : IAsyncPageFilter, IOrderedFilter
    {
        /// <remarks>
        /// Filters on handlers run furthest from the action.
        /// </remarks>t
        public int Order => int.MinValue;

        public Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

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
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

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
}

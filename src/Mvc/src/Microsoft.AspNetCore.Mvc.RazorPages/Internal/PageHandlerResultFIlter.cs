// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageHandlerResultFilter : IAsyncResultFilter, IOrderedFilter
    {
        /// <remarks>
        /// Filters on handlers run furthest from the action.
        /// </remarks>
        public int Order => int.MinValue;

        public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

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
}

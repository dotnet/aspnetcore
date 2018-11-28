// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// A filter implementation which delegates to the controller for action filter interfaces.
    /// </summary>
    public class ControllerActionFilter : IAsyncActionFilter, IOrderedFilter
    {
        // Controller-filter methods run farthest from the action by default.
        /// <inheritdoc />
        public int Order { get; set; } = int.MinValue;

        /// <inheritdoc />
        public Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

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
}
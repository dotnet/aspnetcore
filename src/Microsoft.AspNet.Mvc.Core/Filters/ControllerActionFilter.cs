// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Filters
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
        public async Task OnActionExecutionAsync(
            [NotNull] ActionExecutingContext context,
            [NotNull] ActionExecutionDelegate next)
        {
            var controller = context.Controller;
            if (controller == null)
            {
                throw new InvalidOperationException(Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(context.Controller),
                    nameof(ActionExecutingContext)));
            }

            IAsyncActionFilter asyncActionFilter;
            IActionFilter actionFilter;
            if ((asyncActionFilter = controller as IAsyncActionFilter) != null)
            {
                await asyncActionFilter.OnActionExecutionAsync(context, next);
            }
            else if ((actionFilter = controller as IActionFilter) != null)
            {
                actionFilter.OnActionExecuting(context);
                if (context.Result == null)
                {
                    actionFilter.OnActionExecuted(await next());
                }
            }
            else
            {
                await next();
            }
        }
    }
}
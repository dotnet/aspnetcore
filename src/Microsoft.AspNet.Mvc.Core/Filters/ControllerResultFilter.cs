// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Filters
{
    /// <summary>
    /// A filter implementation which delegates to the controller for result filter interfaces.
    /// </summary>
    public class ControllerResultFilter : IAsyncResultFilter, IOrderedFilter
    {
        // Controller-filter methods run farthest from the result by default.
        /// <inheritdoc />
        public int Order { get; set; } = int.MinValue;

        /// <inheritdoc />
        public async Task OnResultExecutionAsync(
            [NotNull] ResultExecutingContext context,
            [NotNull] ResultExecutionDelegate next)
        {
            var controller = context.Controller;
            if (controller == null)
            {
                throw new InvalidOperationException(Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(context.Controller),
                    nameof(ResultExecutingContext)));
            }

            IAsyncResultFilter asyncResultFilter;
            IResultFilter resultFilter;
            if ((asyncResultFilter = controller as IAsyncResultFilter) != null)
            {
                await asyncResultFilter.OnResultExecutionAsync(context, next);
            }
            else if ((resultFilter = controller as IResultFilter) != null)
            {
                resultFilter.OnResultExecuting(context);
                if (!context.Cancel)
                {
                    resultFilter.OnResultExecuted(await next());
                }
            }
            else
            {
                await next();
            }
        }
    }
}
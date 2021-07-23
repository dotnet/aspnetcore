// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A filter which executes a user configured middleware pipeline.
    /// </summary>
    internal class MiddlewareFilter : IAsyncResourceFilter
    {
        private readonly RequestDelegate _middlewarePipeline;

        public MiddlewareFilter(RequestDelegate middlewarePipeline)
        {
            if (middlewarePipeline == null)
            {
                throw new ArgumentNullException(nameof(middlewarePipeline));
            }

            _middlewarePipeline = middlewarePipeline;
        }

        public Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var httpContext = context.HttpContext;

            // Capture the current context into the feature. This will later be used in the end middleware to continue
            // the execution flow to later MVC layers.
            // Example:
            // this filter -> user-middleware1 -> user-middleware2 -> the-end-middleware -> resource filters or model binding
            var feature = new MiddlewareFilterFeature()
            {
                ResourceExecutionDelegate = next,
                ResourceExecutingContext = context
            };
            httpContext.Features.Set<IMiddlewareFilterFeature>(feature);

            return _middlewarePipeline(httpContext);
        }
    }
}

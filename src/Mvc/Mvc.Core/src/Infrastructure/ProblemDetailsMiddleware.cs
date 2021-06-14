// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// Captures synchronous and asynchronous exceptions from the pipeline and generates error responses.
    /// </summary>
    public class ProblemDetailsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ProblemDetailsOptions _options;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        public ProblemDetailsMiddleware(RequestDelegate next, IOptions<ProblemDetailsOptions> options)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next;
            _options = options.Value;
        }

        /// <summary>
        /// Process an individual request.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            ProblemDetails? problemDetails = null;
            try
            {
                await _next(context);

                if (context.Response.StatusCode == StatusCodes.Status404NotFound)
                {
                    problemDetails = new ProblemDetails()
                    {
                        Detail = "Not found",
                        Title = "404",
                        Status = context.Response.StatusCode,
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
                    };

                    context.Response.Clear();
                }
            }
            catch (Exception ex)
            {
                problemDetails = new ProblemDetails()
                {
                    Detail = _options.ShowStackTrace ? ex.StackTrace : null,
                    Title = "Caught exception",
                    Status = StatusCodes.Status500InternalServerError,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
                };

                context.Response.Clear();
                context.Response.StatusCode = 500;
            }

            if (problemDetails is not null)
            {
                try
                {
                    if (context.Response.HasStarted)
                    {
                        return;
                    }

                    var result = new ObjectResult(problemDetails)
                    {
                        StatusCode = context.Response.StatusCode
                    };

                    var routeData = context.GetRouteData() ?? new RouteData();
                    var actionDescriptor = new ActionDescriptor();
                    var actionContext = new ActionContext(context, routeData, actionDescriptor);

                    await result.ExecuteResultAsync(actionContext);

                    return;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
    }
}

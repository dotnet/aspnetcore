// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class DispatcherMiddleware
    {
        private readonly RequestDelegate _next;

        public DispatcherMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var dictionary = new Dictionary<string, DispatcherFeature>
            {
                {
                    "/example",
                    new DispatcherFeature
                        {
                            Endpoint = new DispatcherEndpoint("example"),
                            RequestDelegate = async (context) =>
                            {
                                await context.Response.WriteAsync("Hello from the example!");
                            }
                        }
                },
                {
                    "/example2",
                    new DispatcherFeature
                        {
                            Endpoint = new DispatcherEndpoint("example2"),
                            RequestDelegate = async (context) =>
                            {
                                await context.Response.WriteAsync("Hello from the second example!");
                            }
                        }
                },
            };

            if (dictionary.TryGetValue(httpContext.Request.Path, out var value))
            {
                var dispatcherFeature = new DispatcherFeature
                {
                    Endpoint = value.Endpoint,
                    RequestDelegate = value.RequestDelegate
                };

                httpContext.Features.Set<IDispatcherFeature>(dispatcherFeature);
                await _next(httpContext);
            }
        }
    }
}

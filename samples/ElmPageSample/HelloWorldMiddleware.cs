// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Logging;

namespace ElmPageSample
{
    public class HelloWorldMiddleware
    {
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;

        public HelloWorldMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<HelloWorldMiddleware>();
        }

        public async Task Invoke(HttpContext httpContext)
        {
            using (_logger.BeginScope("Scope1"))
            {
                _logger.LogVerbose("Getting message");

                httpContext.Response.ContentType = "text/html; charset=utf-8";
                await httpContext.Response.WriteAsync(
                    "<html><body><h2>Hello World!</h2><a href=\"/Elm\">Elm Logs</a></body></html>");
            }
        }
    }
}
using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Logging;

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
            using (_logger.BeginScope("C"))
            {
                _logger.LogVerbose("Getting message");

                await httpContext.Response.WriteAsync("Hello World!");
            }
        }
    }
}
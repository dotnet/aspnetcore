using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.RequestThrottling;


namespace Microsoft.Aspnetcore.RequestThrottling
{
    public class RequestThrottlingMiddleware
    {
        private static SemaphoreWrapper slimshady;

        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public RequestThrottlingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            else if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            _next = next;
            _logger = loggerFactory.CreateLogger<RequestThrottlingMiddleware>();
            
        }

        public async Task Invoke(HttpContext httpContext)
        {
            // put the content here

            _logger.LogDebug("OwO what's this?????");

            await _next(context);
        }
    }
}

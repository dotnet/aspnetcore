using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Logging;

namespace MusicStore
{
    public class MyMiddleware
    {
        private RequestDelegate _next;
        private readonly ILogger<MyMiddleware> _logger;

        public MyMiddleware(RequestDelegate next, ILogger<MyMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var requestBuilder = new StringBuilder();
            requestBuilder.AppendFormat("Request:{0}{1}{2} ||", httpContext.Request.Method, httpContext.Request.Path, httpContext.Request.QueryString);
            foreach (var header in httpContext.Request.Headers)
            {
                requestBuilder.Append(header.Key + ": " + header.Value + " || ");
            }
            _logger.LogWarning(requestBuilder.ToString());

            await _next.Invoke(httpContext);

            var responseBuilder = new StringBuilder();
            responseBuilder.AppendFormat("Response:{0} {1} {2}{3} ||", httpContext.Response.StatusCode, httpContext.Request.Method, httpContext.Request.Path, httpContext.Request.QueryString);
            foreach (var header in httpContext.Response.Headers)
            {
                responseBuilder.Append(header.Key + ": " + header.Value + " || ");
            }
            _logger.LogWarning(responseBuilder.ToString());
        }
    }
}

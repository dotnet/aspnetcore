using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    public class HeaderPropagationLoggerScopeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<HeaderPropagationLoggerScopeMiddleware> _logger;
        private readonly IHeaderPropagationLoggerScopeBuilder _loggerScopeBuilder;

        public HeaderPropagationLoggerScopeMiddleware(RequestDelegate next, ILogger<HeaderPropagationLoggerScopeMiddleware> logger, IHeaderPropagationLoggerScopeBuilder loggerScopeBuilder)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerScopeBuilder = loggerScopeBuilder ?? throw new ArgumentNullException(nameof(loggerScopeBuilder));
        }

        public Task Invoke(HttpContext context)
        {
            using (_logger.BeginScope(_loggerScopeBuilder.Build()))
            {
                return _next.Invoke(context);
            }
        }
    }
}

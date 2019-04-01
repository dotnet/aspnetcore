using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Diagnostics
{
    public class ErrorContext
    {
        public ErrorContext(HttpContext httpContext, Exception exception)
        {
            HttpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        public HttpContext HttpContext { get; }
        public Exception Exception { get; }
    }
}

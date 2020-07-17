using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Csp
{
    public class CspMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ContentSecurityPolicy _csp;

        public CspMiddleware(RequestDelegate next, ContentSecurityPolicy csp)
        {
            _next = next;
            _csp = csp;
        }

        public Task Invoke(HttpContext context, INonce nonce)
        {
            if (context.Request.ContentType == null || context.Request.ContentType.Equals("text/html"))
            {
                context.Response.Headers[_csp.GetHeaderName()] = _csp.GetPolicy(nonce);
            }
            return _next(context);
        }
    }
}

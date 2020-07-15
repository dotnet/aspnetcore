using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Csp
{
    public class CspMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ICspService _cspService;
        private readonly ContentSecurityPolicy _csp;

        public CspMiddleware(RequestDelegate next, ICspService cspService, ContentSecurityPolicy csp)
        {
            _next = next;
            _cspService = cspService;
            _csp = csp;
        }

        public Task Invoke(HttpContext context)
        {
            context.Response.Headers[CspConstants.CspEnforcedHeaderName] = _csp.GetPolicy();
            return _next(context);
        }
    }
}

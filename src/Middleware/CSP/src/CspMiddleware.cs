using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Csp
{
    public class CspMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ICspService _cspService;

        public CspMiddleware(RequestDelegate next, ICspService cspService)
        {
            _next = next;
            _cspService = cspService;
        }

        public Task Invoke(HttpContext context, IContentSecurityPolicyProvider cspProvider)
        {
            return _next(context);
        }
    }
}

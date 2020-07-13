using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Csp
{
    public class CspMiddleware
    {


        public Task Invoke(HttpContext context, IContentSecurityPolicyProvider cspProvider)
        {

        }
    }
}

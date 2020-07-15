using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Csp
{
    public class DefaultContentSecurityPolicyProvider : IContentSecurityPolicyProvider
    {
        public Task<ContentSecurityPolicy> GetPolicyAsync(HttpContext context)
        {
            throw new NotImplementedException();
        }
    }
}

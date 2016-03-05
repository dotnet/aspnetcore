using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Authentication
{
    public class ClaimsTransformationContext
    {
        public ClaimsTransformationContext(HttpContext context)
        {
            Context = context;
        }
        public HttpContext Context { get; }
        public ClaimsPrincipal Principal { get; set; }
    }
}

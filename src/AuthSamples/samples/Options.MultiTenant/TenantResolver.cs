using Microsoft.AspNetCore.Http;

namespace AuthSamples.Options.MultiTenant
{
    public class TenantResolver
    {
        private readonly IHttpContextAccessor _context;

        public TenantResolver(IHttpContextAccessor context) => _context = context;

        public string ResolveTenant()
            => _context.HttpContext.Request.Query["tenant"].ToString();
    }
}

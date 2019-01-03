using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace AuthSamples.Options.MultiTenant.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly TenantOptionsCache _cache;
        private readonly TenantResolver _resolver;

        public AuthController(IAuthenticationSchemeProvider schemeProvider, TenantOptionsCache optionsCache, TenantResolver resolver)
        {
            _schemeProvider = schemeProvider;
            _cache = optionsCache;
            _resolver = resolver;
        }

        public IActionResult Remove(string scheme)
        {
            _schemeProvider.RemoveScheme(scheme);
            _cache.Remove(scheme);
            return Redirect($"/?tenant={_resolver.ResolveTenant()}");
        }

        [HttpPost]
        public async Task<IActionResult> AddOrUpdate(string scheme, string clientId, string clientSecret)
        {
            if (await _schemeProvider.GetSchemeAsync(scheme) == null)
            {
                _schemeProvider.AddScheme(new AuthenticationScheme(scheme, scheme, typeof(SimpleAuthHandler)));
            }
            _cache.Update(scheme, new SimpleOptions { ClientId = clientId, ClientSecret = clientSecret });
            return Redirect($"/?tenant={_resolver.ResolveTenant()}");
        }
    }
}

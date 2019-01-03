using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace AuthSamples.Options.MultiTenant
{
    public class TenantSchemeResolver : IAuthenticationSchemeProvider
    {
        private readonly TenantResolver _resolver;
        private Dictionary<string, Dictionary<string, AuthenticationScheme>> _map = new Dictionary<string, Dictionary<string, AuthenticationScheme>>();

        public TenantSchemeResolver(TenantResolver resolver) => _resolver = resolver;

        private Dictionary<string, AuthenticationScheme> GetMap(string tenant)
        {
            if (!_map.ContainsKey(tenant))
            {
                _map[tenant] = new Dictionary<string, AuthenticationScheme>();
            }
            return _map[tenant];

        }

        public void AddScheme(AuthenticationScheme scheme)
            => GetMap(_resolver.ResolveTenant())[scheme.Name] = scheme;

        public Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync()
            => Task.FromResult<IEnumerable<AuthenticationScheme>>(GetMap(_resolver.ResolveTenant()).Values);

        public Task<AuthenticationScheme> GetDefaultAuthenticateSchemeAsync()
            => null;

        public Task<AuthenticationScheme> GetDefaultChallengeSchemeAsync()
            => null;

        public Task<AuthenticationScheme> GetDefaultForbidSchemeAsync()
            => null;

        public Task<AuthenticationScheme> GetDefaultSignInSchemeAsync()
            => null;

        public Task<AuthenticationScheme> GetDefaultSignOutSchemeAsync()
            => null;

        public Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync()
            => null;

        public Task<AuthenticationScheme> GetSchemeAsync(string name)
        {
            var map = GetMap(_resolver.ResolveTenant()).TryGetValue(name, out var val);
            return Task.FromResult(val);
        }

        public void RemoveScheme(string name)
            => GetMap(_resolver.ResolveTenant()).Remove(name);
    }
}

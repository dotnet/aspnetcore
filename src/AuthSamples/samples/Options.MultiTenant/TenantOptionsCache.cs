using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace AuthSamples.Options.MultiTenant
{
    public class TenantOptionsCache
    {
        private readonly Dictionary<string, OptionsCache<SimpleOptions>> _cache = new Dictionary<string, OptionsCache<SimpleOptions>>();
        private readonly TenantResolver _resolver;

        public TenantOptionsCache(TenantResolver resolver)
        {
            _resolver = resolver;
        }

        public OptionsCache<SimpleOptions> GetCache()
        {
            var tenant = _resolver.ResolveTenant();
            if (!_cache.ContainsKey(tenant))
            {
                _cache[tenant] = new OptionsCache<SimpleOptions>();
            }
            return _cache[tenant];
        }

        public void Remove(string name)
        {
            var cache = GetCache();
            cache.TryRemove(name);
        }

        public void Update(string name, SimpleOptions options)
        {
            var cache = GetCache();
            cache.TryRemove(name);
            cache.TryAdd(name, options);
        }
    }
}

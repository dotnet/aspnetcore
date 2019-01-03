using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace AuthSamples.Options.MultiTenant
{
    public class TenantOptionsMonitor : IOptionsMonitor<SimpleOptions>
    {
        private readonly TenantResolver _resolver;
        private readonly IOptionsFactory<SimpleOptions> _factory;
        private readonly TenantOptionsCache _cache;

        public TenantOptionsMonitor(TenantResolver resolver, IOptionsFactory<SimpleOptions> factory, TenantOptionsCache cache)
        {
            _factory = factory;
            _resolver = resolver;
            _cache = cache;
        }

        public SimpleOptions CurrentValue => Get(Microsoft.Extensions.Options.Options.DefaultName);

        public SimpleOptions Get(string name)
            => _cache.GetCache().GetOrAdd(name, () => _factory.Create(name));

        // This sample doesn't support change notifications
        public IDisposable OnChange(Action<SimpleOptions, string> listener)
        {
            throw new NotImplementedException();
        }
    }
}

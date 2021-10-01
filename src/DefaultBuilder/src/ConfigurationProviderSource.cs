// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Builder
{
    internal sealed class ConfigurationProviderSource : IConfigurationSource
    {
        private readonly IConfigurationProvider _configurationProvider;

        public ConfigurationProviderSource(IConfigurationProvider configurationProvider)
        {
            _configurationProvider = configurationProvider;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new IgnoreFirstLoadConfigurationProvider(_configurationProvider);
        }

        // These providers have already been loaded, so no need to reload initially.
        // Otherwise, providers that cannot be reloaded like StreamConfigurationProviders will fail.
        private sealed class IgnoreFirstLoadConfigurationProvider : IConfigurationProvider, IEnumerable<IConfigurationProvider>, IDisposable
        {
            private readonly IConfigurationProvider _provider;

            private bool _hasIgnoredFirstLoad;

            public IgnoreFirstLoadConfigurationProvider(IConfigurationProvider provider)
            {
                _provider = provider;
            }

            public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
            {
                return _provider.GetChildKeys(earlierKeys, parentPath);
            }

            public IChangeToken GetReloadToken()
            {
                return _provider.GetReloadToken();
            }

            public void Load()
            {
                if (!_hasIgnoredFirstLoad)
                {
                    _hasIgnoredFirstLoad = true;
                    return;
                }

                _provider.Load();
            }

            public void Set(string key, string value)
            {
                _provider.Set(key, value);
            }

            public bool TryGet(string key, out string value)
            {
                return _provider.TryGet(key, out value);
            }

            // Provide access to the original IConfigurationProvider via a single-element IEnumerable to code that goes out of its way to look for it.
            public IEnumerator<IConfigurationProvider> GetEnumerator() => GetUnwrappedEnumerable().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetUnwrappedEnumerable().GetEnumerator();

            public override bool Equals(object? obj)
            {
                return _provider.Equals(obj);
            }

            public override int GetHashCode()
            {
                return _provider.GetHashCode();
            }

            public override string? ToString()
            {
                return _provider.ToString();
            }

            public void Dispose()
            {
                (_provider as IDisposable)?.Dispose();
            }

            private IEnumerable<IConfigurationProvider> GetUnwrappedEnumerable()
            {
                yield return _provider;
            }
        }
    }
}

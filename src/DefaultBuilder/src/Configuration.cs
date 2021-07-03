// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Configuration is mutable configuration object. It is both an <see cref="IConfigurationBuilder"/> and an <see cref="IConfigurationRoot"/>.
    /// As sources are added, it updates its current view of configuration. Once Build is called, configuration is frozen.
    /// </summary>
    public sealed class Configuration : IConfigurationRoot, IConfigurationBuilder, IDisposable
    {
        private readonly ConfigurationSources _sources;
        private readonly ConfigurationBuilderProperties _properties;

        private readonly object _providerLock = new();
        private readonly List<IConfigurationProvider> _providers = new();
        private readonly List<IDisposable> _changeTokenRegistrations = new();
        private ConfigurationReloadToken _changeToken = new();

        /// <summary>
        /// Creates an empty mutable configuration object that is both an <see cref="IConfigurationBuilder"/> and an <see cref="IConfigurationRoot"/>.
        /// </summary>
        public Configuration()
        {
            _sources = new ConfigurationSources(this);
            _properties = new ConfigurationBuilderProperties(this);

            // Make sure there's some default storage since there are no default providers.
            this.AddInMemoryCollection();

            NotifySourceAdded(_sources[0]);
        }

        /// <inheritdoc/>
        public string? this[string key]
        {
            get
            {
                lock (_providerLock)
                {
                    for (int i = _providers.Count - 1; i >= 0; i--)
                    {
                        var provider = _providers[i];

                        if (provider.TryGet(key, out string value))
                        {
                            return value;
                        }
                    }

                    return null;
                }
            }
            set
            {
                lock (_providerLock)
                {
                    if (_providers.Count == 0)
                    {
                        throw new InvalidOperationException("A configuration source is not registered. Please register one before setting a value.");
                    }

                    foreach (var provider in _providers)
                    {
                        provider.Set(key, value);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public IConfigurationSection GetSection(string key) => new ConfigurationSection(this, key);

        /// <inheritdoc/>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            lock (_providerLock)
            {
                // ToList() to eagerly evaluate inside lock.
                return _providers
                    .Aggregate(Enumerable.Empty<string>(),
                        static (seed, source) => source.GetChildKeys(seed, parentPath: null))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(GetSection)
                    .ToList();
            }
        }

        IDictionary<string, object> IConfigurationBuilder.Properties => _properties;

        IList<IConfigurationSource> IConfigurationBuilder.Sources => _sources;

        /// <inheritdoc/>
        IEnumerable<IConfigurationProvider> IConfigurationRoot.Providers
        {
            get
            {
                lock (_providerLock)
                {
                    return new List<IConfigurationProvider>(_providers);
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (_providerLock)
            {
                DisposeRegistrationsAndProvidersUnsynchronized();
            }
        }

        IConfigurationBuilder IConfigurationBuilder.Add(IConfigurationSource source)
        {
            _sources.Add(source ?? throw new ArgumentNullException(nameof(source)));
            return this;
        }

        IConfigurationRoot IConfigurationBuilder.Build() => this;

        IChangeToken IConfiguration.GetReloadToken() => _changeToken;

        void IConfigurationRoot.Reload()
        {
            lock (_providerLock)
            {
                foreach (var provider in _providers)
                {
                    provider.Load();
                }
            }

            RaiseChanged();
        }

        private void RaiseChanged()
        {
            var previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
            previousToken.OnReload();
        }

        // Don't rebuild and reload all providers in the common case when a source is simply added to the IList.
        private void NotifySourceAdded(IConfigurationSource source)
        {
            lock (_providerLock)
            {
                var provider = source.Build(this);
                _providers.Add(provider);

                provider.Load();
                _changeTokenRegistrations.Add(ChangeToken.OnChange(() => provider.GetReloadToken(), () => RaiseChanged()));
            }

            RaiseChanged();
        }

        // Something other than Add was called on IConfigurationBuilder.Sources or the FileProvider has been set to a new value.
        // This is unusual, so we don't bother optimizing it.
        private void NotifySourcesChanged()
        {
            lock (_providerLock)
            {
                DisposeRegistrationsAndProvidersUnsynchronized();

                _changeTokenRegistrations.Clear();
                _providers.Clear();

                foreach (var source in _sources)
                {
                    _providers.Add(source.Build(this));
                }

                foreach (var p in _providers)
                {
                    p.Load();
                    _changeTokenRegistrations.Add(ChangeToken.OnChange(() => p.GetReloadToken(), () => RaiseChanged()));
                }
            }

            RaiseChanged();
        }

        private void DisposeRegistrationsAndProvidersUnsynchronized()
        {
            // dispose change token registrations
            foreach (var registration in _changeTokenRegistrations)
            {
                registration.Dispose();
            }

            // dispose providers
            foreach (var provider in _providers)
            {
                (provider as IDisposable)?.Dispose();
            }
        }

        private class ConfigurationSources : IList<IConfigurationSource>
        {
            private readonly List<IConfigurationSource> _sources = new();
            private readonly Configuration _config;

            public ConfigurationSources(Configuration config)
            {
                _config = config;
            }

            public IConfigurationSource this[int index]
            {
                get => _sources[index];
                set
                {
                    _sources[index] = value;
                    _config.NotifySourcesChanged();
                }
            }

            public int Count => _sources.Count;

            public bool IsReadOnly => false;

            public void Add(IConfigurationSource source)
            {
                _sources.Add(source);
                _config.NotifySourceAdded(source);
            }

            public void Clear()
            {
                _sources.Clear();
                _config.NotifySourcesChanged();
            }

            public bool Contains(IConfigurationSource source)
            {
                return _sources.Contains(source);
            }

            public void CopyTo(IConfigurationSource[] array, int arrayIndex)
            {
                _sources.CopyTo(array, arrayIndex);
            }

            public IEnumerator<IConfigurationSource> GetEnumerator()
            {
                return _sources.GetEnumerator();
            }

            public int IndexOf(IConfigurationSource source)
            {
                return _sources.IndexOf(source);
            }

            public void Insert(int index, IConfigurationSource source)
            {
                _sources.Insert(index, source);
                _config.NotifySourcesChanged();
            }

            public bool Remove(IConfigurationSource source)
            {
                var removed = _sources.Remove(source);
                _config.NotifySourcesChanged();
                return removed;
            }

            public void RemoveAt(int index)
            {
                _sources.RemoveAt(index);
                _config.NotifySourcesChanged();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class ConfigurationBuilderProperties : IDictionary<string, object>
        {
            private const string FileProviderKey = "FileProvider";

            private readonly Dictionary<string, object> _properties = new();
            private readonly Configuration _config;
            private object? _fileProvider;

            public ConfigurationBuilderProperties(Configuration config)
            {
                _config = config;
            }

            public object this[string key]
            {
                get => _properties[key];
                set
                {
                    _properties[key] = value;
                    CheckForFileProviderChange(key, value);
                }
            }

            public ICollection<string> Keys => _properties.Keys;

            public ICollection<object> Values => _properties.Values;

            public int Count => _properties.Count;

            public bool IsReadOnly => false;

            public void Add(string key, object value)
            {
                _properties.Add(key, value);
                CheckForFileProviderChange(key, value);
            }

            public void Add(KeyValuePair<string, object> item)
            {
                ((IDictionary<string, object>)_properties).Add(item);
                CheckForFileProviderChange(item.Key, item.Value);
            }

            public void Clear()
            {
                _properties.Clear();
                CheckForFileProviderChange(FileProviderKey, null);
            }

            public bool Contains(KeyValuePair<string, object> item)
            {
                return _properties.Contains(item);
            }

            public bool ContainsKey(string key)
            {
                return _properties.ContainsKey(key);
            }

            public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
            {
                ((IDictionary<string, object>)_properties).CopyTo(array, arrayIndex);
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                return _properties.GetEnumerator();
            }

            public bool Remove(string key)
            {
                var wasRemoved = _properties.Remove(key);
                CheckForFileProviderChange(key, null);
                return wasRemoved;
            }

            public bool Remove(KeyValuePair<string, object> item)
            {
                var wasRemoved = ((IDictionary<string, object>)_properties).Remove(item);
                CheckForFileProviderChange(item.Key, null);
                return wasRemoved;
            }

            public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
            {
                return _properties.TryGetValue(key, out value);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _properties.GetEnumerator();
            }

            private void CheckForFileProviderChange(string key, object? value)
            {
                if (key == FileProviderKey && !ReferenceEquals(value, _fileProvider))
                {
                    _fileProvider = value;
                    // Reload all the sources since the base path has likely changed.
                    _config.NotifySourcesChanged();
                }
            }
        }
    }
}

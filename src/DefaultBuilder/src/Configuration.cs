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
        private ConfigurationRoot _configurationRoot;

        private ConfigurationReloadToken _changeToken = new();
        private IDisposable? _changeTokenRegistration;

        /// <summary>
        /// Creates an empty mutable configuration object that is both an <see cref="IConfigurationBuilder"/> and an <see cref="IConfigurationRoot"/>.
        /// </summary>
        public Configuration()
        {
            _sources = new ConfigurationSources(this);

            // Make sure there's some default storage since there are no default providers.
            this.AddInMemoryCollection();

            Update();
        }

        /// <summary>
        /// Automatically update the <see cref="IConfiguration"/> on <see cref="IConfigurationBuilder"/> changes.
        /// If <see langword="false"/>, <see cref="Update()"/> will manually update the <see cref="IConfiguration"/>.
        /// </summary>
        internal bool AutoUpdate { get; set; } = true;

        /// <inheritdoc />
        public string this[string key] { get => _configurationRoot[key]; set => _configurationRoot[key] = value; }

        /// <inheritdoc />
        public IConfigurationSection GetSection(string key) => new ConfigurationSection(this, key);

        /// <inheritdoc />
        public IEnumerable<IConfigurationSection> GetChildren() => GetChildrenImplementation(null);

        IDictionary<string, object> IConfigurationBuilder.Properties { get; } = new Dictionary<string, object>();

        IList<IConfigurationSource> IConfigurationBuilder.Sources => _sources;

        IEnumerable<IConfigurationProvider> IConfigurationRoot.Providers => _configurationRoot.Providers;

        /// <summary>
        /// Manually update the <see cref="IConfiguration"/> to reflect <see cref="IConfigurationBuilder"/> changes.
        /// It is not necessary to call this if <see cref="AutoUpdate"/> is <see langword="true"/>.
        /// </summary>
        [MemberNotNull(nameof(_configurationRoot))]
        internal void Update()
        {
            var newConfiguration = BuildConfigurationRoot();
            var prevConfiguration = _configurationRoot;

            _configurationRoot = newConfiguration;

            _changeTokenRegistration?.Dispose();
            (prevConfiguration as IDisposable)?.Dispose();

            _changeTokenRegistration = ChangeToken.OnChange(() => newConfiguration.GetReloadToken(), RaiseChanged);
            RaiseChanged();
        }

        /// <inheritdoc />
        void IDisposable.Dispose()
        {
            _changeTokenRegistration?.Dispose();
            _configurationRoot?.Dispose();
        }

        IConfigurationBuilder IConfigurationBuilder.Add(IConfigurationSource source)
        {
            _sources.Add(source ?? throw new ArgumentNullException(nameof(source)));
            return this;
        }

        IConfigurationRoot IConfigurationBuilder.Build() => BuildConfigurationRoot();

        IChangeToken IConfiguration.GetReloadToken() => _changeToken;

        void IConfigurationRoot.Reload() => _configurationRoot.Reload();

        private void NotifySourcesChanged()
        {
            if (AutoUpdate)
            {
                Update();
            }
        }

        private ConfigurationRoot BuildConfigurationRoot()
        {
            var providers = new List<IConfigurationProvider>();
            foreach (var source in _sources)
            {
                var provider = source.Build(this);
                providers.Add(provider);
            }
            return new ConfigurationRoot(providers);
        }

        private void RaiseChanged()
        {
            var previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
            previousToken.OnReload();
        }

        /// <summary>
        /// Gets the immediate children sub-sections of configuration root based on key.
        /// </summary>
        /// <param name="path">Key of a section of which children to retrieve.</param>
        /// <returns>Immediate children sub-sections of section specified by key.</returns>
        private IEnumerable<IConfigurationSection> GetChildrenImplementation(string? path)
        {
            // From https://github.com/dotnet/runtime/blob/01b7e73cd378145264a7cb7a09365b41ed42b240/src/libraries/Microsoft.Extensions.Configuration/src/InternalConfigurationRootExtensions.cs
            return _configurationRoot.Providers
                .Aggregate(Enumerable.Empty<string>(),
                    (seed, source) => source.GetChildKeys(seed, path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(key => _configurationRoot.GetSection(path == null ? key : ConfigurationPath.Combine(path, key)));
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

            public void Add(IConfigurationSource item)
            {
                _sources.Add(item);
                _config.NotifySourcesChanged();
            }

            public void Clear()
            {
                _sources.Clear();
                _config.NotifySourcesChanged();
            }

            public bool Contains(IConfigurationSource item)
            {
                return _sources.Contains(item);
            }

            public void CopyTo(IConfigurationSource[] array, int arrayIndex)
            {
                _sources.CopyTo(array, arrayIndex);
            }

            public IEnumerator<IConfigurationSource> GetEnumerator()
            {
                return _sources.GetEnumerator();
            }

            public int IndexOf(IConfigurationSource item)
            {
                return _sources.IndexOf(item);
            }

            public void Insert(int index, IConfigurationSource item)
            {
                _sources.Insert(index, item);
                _config.NotifySourcesChanged();
            }

            public bool Remove(IConfigurationSource item)
            {
                var removed = _sources.Remove(item);
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
    }
}

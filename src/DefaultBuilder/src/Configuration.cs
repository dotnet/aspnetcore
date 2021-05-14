// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Builder
{
    ///// <summary>
    ///// Configuration is mutable configuration object. It is both a configuration builder and an IConfigurationRoot. 
    ///// As sources are added, it updates its current view of configuration. Once Build is called, configuration is frozen.
    ///// </summary>
    //public sealed class Configuration : IConfigurationRoot, IConfigurationBuilder
    //{
    //    private readonly ConfigurationBuilder _builder = new();
    //    private IConfigurationRoot _configuration;

    //    /// <summary>
    //    /// Gets or sets a configuration value.
    //    /// </summary>
    //    /// <param name="key">The configuration key.</param>
    //    /// <returns>The configuration value.</returns>
    //    public string this[string key] { get => _configuration[key]; set => _configuration[key] = value; }

    //    /// <summary>
    //    /// Gets a configuration sub-section with the specified key.
    //    /// </summary>
    //    /// <param name="key">The key of the configuration section.</param>
    //    /// <returns>The <see cref="IConfigurationSection"/>.</returns>
    //    /// <remarks>
    //    ///     This method will never return <c>null</c>. If no matching sub-section is found with the specified key,
    //    ///     an empty <see cref="IConfigurationSection"/> will be returned.
    //    /// </remarks>
    //    public IConfigurationSection GetSection(string key)
    //    {
    //        return _configuration.GetSection(key);
    //    }

    //    /// <summary>
    //    /// Gets the immediate descendant configuration sub-sections.
    //    /// </summary>
    //    /// <returns>The configuration sub-sections.</returns>
    //    public IEnumerable<IConfigurationSection> GetChildren() => _configuration.GetChildren();

    //    IDictionary<string, object> IConfigurationBuilder.Properties => _builder.Properties;

    //    IList<IConfigurationSource> IConfigurationBuilder.Sources => Sources;

    //    internal IList<IConfigurationSource> Sources { get; }

    //    IEnumerable<IConfigurationProvider> IConfigurationRoot.Providers => _configuration.Providers;

    //    /// <summary>
    //    /// Creates a new <see cref="Configuration"/>.
    //    /// </summary>
    //    public Configuration()
    //    {
    //        _configuration = _builder.Build();
    //        Sources = new ConfigurationSources(_builder.Sources, this);
    //    }

    //    internal void ChangeBasePath(string path)
    //    {
    //        this.SetBasePath(path);
    //        UpdateConfiguration();
    //    }

    //    internal void ChangeFileProvider(IFileProvider fileProvider)
    //    {
    //        this.SetFileProvider(fileProvider);
    //        UpdateConfiguration();
    //    }

    //    //private void AttachReloadToken()
    //    //{
    //    //    // We dispose the IConfigurationRoot every time we reattach so we don't bother tracking the registration.
    //    //    _configuration.GetReloadToken().RegisterChangeCallback(static state =>
    //    //        ((ConfigurationReloadToken)state).OnReload(), _reloadToken);
    //    //}

    //    private void UpdateConfiguration()
    //    {
    //        var current = _configuration;
    //        if (current is IDisposable disposable)
    //        {
    //            disposable.Dispose();
    //        }
    //        _configuration = _builder.Build();
    //    }

    //    IConfigurationBuilder IConfigurationBuilder.Add(IConfigurationSource source)
    //    {
    //        Sources.Add(source);
    //        return this;
    //    }

    //    IConfigurationRoot IConfigurationBuilder.Build()
    //    {
    //        // No more modification is expected after this final build
    //        UpdateConfiguration();
    //        return this;
    //    }

    //    IChangeToken IConfiguration.GetReloadToken()
    //    {
    //        return _configuration.GetReloadToken();
    //    }

    //    void IConfigurationRoot.Reload()
    //    {
    //        _configuration.Reload();
    //        UpdateConfiguration();
    //    }

    //    // On source modifications, we rebuild configuration
    //    private class ConfigurationSources : IList<IConfigurationSource>
    //    {
    //        private readonly IList<IConfigurationSource> _sources;
    //        private readonly Configuration _config;

    //        public ConfigurationSources(IList<IConfigurationSource> sources, Configuration config)
    //        {
    //            _sources = sources;
    //            _config = config;
    //        }

    //        public IConfigurationSource this[int index]
    //        {
    //            get => _sources[index];
    //            set
    //            {
    //                _sources[index] = value;
    //                _config.UpdateConfiguration();
    //            }
    //        }

    //        public int Count => _sources.Count;

    //        public bool IsReadOnly => _sources.IsReadOnly;

    //        public void Add(IConfigurationSource item)
    //        {
    //            _sources.Add(item);
    //            _config.UpdateConfiguration();
    //        }

    //        public void Clear()
    //        {
    //            _sources.Clear();
    //            _config.UpdateConfiguration();
    //        }

    //        public bool Contains(IConfigurationSource item)
    //        {
    //            return _sources.Contains(item);
    //        }

    //        public void CopyTo(IConfigurationSource[] array, int arrayIndex)
    //        {
    //            _sources.CopyTo(array, arrayIndex);
    //        }

    //        public IEnumerator<IConfigurationSource> GetEnumerator()
    //        {
    //            return _sources.GetEnumerator();
    //        }

    //        public int IndexOf(IConfigurationSource item)
    //        {
    //            return _sources.IndexOf(item);
    //        }

    //        public void Insert(int index, IConfigurationSource item)
    //        {
    //            _sources.Insert(index, item);
    //            _config.UpdateConfiguration();
    //        }

    //        public bool Remove(IConfigurationSource item)
    //        {
    //            var removed = _sources.Remove(item);
    //            _config.UpdateConfiguration();
    //            return removed;
    //        }

    //        public void RemoveAt(int index)
    //        {
    //            _sources.RemoveAt(index);
    //            _config.UpdateConfiguration();
    //        }

    //        IEnumerator IEnumerable.GetEnumerator()
    //        {
    //            return GetEnumerator();
    //        }
    //    }

    /// <summary>
    /// Configuration is mutable configuration object. It is both a configuration builder and an IConfigurationRoot. 
    /// As sources are added, it updates its current view of configuration. Once Build is called, configuration is frozen.
    /// </summary>
    public sealed class Configuration : IConfigurationRoot, IConfigurationBuilder, IDisposable
    {
        // We start off dirty to ensure the first configuration root is built on demand
        private bool _dirty = true;
        private ConfigurationRoot? _configurationRoot;
        private ConfigurationReloadToken _changeToken = new();
        private IDisposable? _changeTokenRegistration;

        // Needs to be a separate field, as it is not possible to initialize an explicitly implemented property in the constructor
        // and we pass an instance method to the constructor used to initialize it, so cannot use a field initializer.
        // On the other hand, if this is a separate class (not an enhanced version of the existing ConfigurationBuilder class)
        // then we probably do want `Properties` to be an explicit interface implementation, so users looking to read configuration values
        // don't get confused by it. So explicit backing field it is!
        private readonly IDictionary<string, object> _properties;

        private IConfigurationRoot ConfigurationRoot
        {
            get
            {
                EnsureFreshConfiguration();
                Debug.Assert(_configurationRoot is not null);
                return _configurationRoot;
            }
        }

        public string this[string key] { get => ConfigurationRoot[key]; set => ConfigurationRoot[key] = value; }

        public IConfigurationSection GetSection(string key) => new ConfigurationSection(this, key);

        public IEnumerable<IConfigurationSection> GetChildren() => GetChildrenImplementation(null);

        IDictionary<string, object> IConfigurationBuilder.Properties => _properties;

        internal IList<IConfigurationSource> Sources { get; }
        IList<IConfigurationSource> IConfigurationBuilder.Sources => Sources;

        IEnumerable<IConfigurationProvider> IConfigurationRoot.Providers => ConfigurationRoot.Providers;

        public Configuration()
        {
            _properties = new ConfigurationProperties(this);
            Sources = new ConfigurationSources(this);
        }

        public void Dispose()
        {
            _changeTokenRegistration?.Dispose();
            _configurationRoot?.Dispose();
        }

        IConfigurationBuilder IConfigurationBuilder.Add(IConfigurationSource source)
        {
            Sources.Add(source ?? throw new ArgumentNullException(nameof(source)));
            return this;
        }

        IConfigurationRoot IConfigurationBuilder.Build() => BuildConfigurationRoot();

        IChangeToken IConfiguration.GetReloadToken() => _changeToken;

        void IConfigurationRoot.Reload() => ConfigurationRoot.Reload();

        private void MarkDirty() => _dirty = true;

        private void EnsureFreshConfiguration()
        {
            if (!_dirty)
            {
                return;
            }

            var newConfiguration = BuildConfigurationRoot();
            var prevConfiguration = _configurationRoot;

            _configurationRoot = newConfiguration;
            _dirty = false;

            _changeTokenRegistration?.Dispose();
            (prevConfiguration as IDisposable)?.Dispose();

            _changeTokenRegistration = ChangeToken.OnChange(() => newConfiguration.GetReloadToken(), RaiseChanged);
            RaiseChanged();
        }

        private ConfigurationRoot BuildConfigurationRoot()
        {
            var providers = new List<IConfigurationProvider>();
            foreach (var source in Sources)
            {
                IConfigurationProvider provider = source.Build(this);
                providers.Add(provider);
            }
            return new ConfigurationRoot(providers);
        }

        private void RaiseChanged()
        {
            ConfigurationReloadToken previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
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
            return ConfigurationRoot.Providers
                .Aggregate(Enumerable.Empty<string>(),
                    (seed, source) => source.GetChildKeys(seed, path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(key => ConfigurationRoot.GetSection(path == null ? key : ConfigurationPath.Combine(path, key)));
        }

        // On source modifications, we mark configuration as dirty
        private class ConfigurationSources : IList<IConfigurationSource>
        {
            private readonly IList<IConfigurationSource> _sources;
            private readonly Configuration _config;

            public ConfigurationSources(Configuration config)
            {
                _sources = new List<IConfigurationSource>();
                _config = config;
            }

            public IConfigurationSource this[int index]
            {
                get => _sources[index];
                set
                {
                    _sources[index] = value;
                    _config.MarkDirty();
                }
            }

            public int Count => _sources.Count;

            public bool IsReadOnly => _sources.IsReadOnly;

            public void Add(IConfigurationSource item)
            {
                _sources.Add(item);
                _config.MarkDirty();
            }

            public void Clear()
            {
                _sources.Clear();
                _config.MarkDirty();
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
                _config.MarkDirty();
            }

            public bool Remove(IConfigurationSource item)
            {
                var removed = _sources.Remove(item);
                _config.MarkDirty();
                return removed;
            }

            public void RemoveAt(int index)
            {
                _sources.RemoveAt(index);
                _config.MarkDirty();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        // On property modifications we mark configuration as dirty.
        private class ConfigurationProperties : IDictionary<string, object>
        {
            private readonly IDictionary<string, object> _properties = new Dictionary<string, object>();
            private readonly Configuration _config;

            public ConfigurationProperties(Configuration config)
            {
                _config = config;
            }

            public object this[string key]
            {
                get => _properties[key];
                set
                {
                    _config.MarkDirty();
                    _properties[key] = value;
                }
            }

            public ICollection<string> Keys => _properties.Keys;

            public ICollection<object> Values => _properties.Values;

            public int Count => _properties.Count;

            public bool IsReadOnly => false;

            public void Add(string key, object value)
            {
                _properties.Add(key, value);
                _config.MarkDirty();
            }

            public void Add(KeyValuePair<string, object> item)
            {
                _properties.Add(item);
                _config.MarkDirty();
            }

            public void Clear()
            {
                _properties.Clear();
                _config.MarkDirty();
            }

            public bool Contains(KeyValuePair<string, object> item) => _properties.Contains(item);

            public bool ContainsKey(string key) => _properties.ContainsKey(key);

            public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => _properties.CopyTo(array, arrayIndex);

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _properties.GetEnumerator();

            public bool Remove(string key)
            {
                var removed = _properties.Remove(key);
                _config.MarkDirty();
                return removed;
            }

            public bool Remove(KeyValuePair<string, object> item)
            {
                var removed = _properties.Remove(item);
                _config.MarkDirty();
                return removed;
            }

            public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value) => _properties.TryGetValue(key, out value);
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_properties).GetEnumerator();
        }
    }
}

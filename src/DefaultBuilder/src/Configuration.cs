// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

// TODO: Microsft.Extensions.Configuration API Proposal
namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Configuration is mutable configuration object. It is both a configuration builder and an IConfigurationRoot. 
    /// As sources are added, it updates its current view of configuration. Once Build is called, configuration is frozen.
    /// </summary>
    public class Configuration : IConfigurationRoot, IConfigurationBuilder
    {
        private readonly ConfigurationBuilder _builder = new();
        private IConfigurationRoot _configuration;

        /// <summary>
        /// Gets or sets a configuration value.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The configuration value.</returns>
        public string this[string key] { get => _configuration[key]; set => _configuration[key] = value; }

        /// <summary>
        /// Gets a configuration sub-section with the specified key.
        /// </summary>
        /// <param name="key">The key of the configuration section.</param>
        /// <returns>The <see cref="IConfigurationSection"/>.</returns>
        /// <remarks>
        ///     This method will never return <c>null</c>. If no matching sub-section is found with the specified key,
        ///     an empty <see cref="IConfigurationSection"/> will be returned.
        /// </remarks>
        public IConfigurationSection GetSection(string key)
        {
            return _configuration.GetSection(key);
        }

        /// <summary>
        /// Gets the immediate descendant configuration sub-sections.
        /// </summary>
        /// <returns>The configuration sub-sections.</returns>
        public IEnumerable<IConfigurationSection> GetChildren() => _configuration.GetChildren();

        IDictionary<string, object> IConfigurationBuilder.Properties => _builder.Properties;

        // TODO: Handle modifications to Sources and keep the configuration root in sync
        IList<IConfigurationSource> IConfigurationBuilder.Sources => Sources;

        internal IList<IConfigurationSource> Sources { get; }

        IEnumerable<IConfigurationProvider> IConfigurationRoot.Providers => _configuration.Providers;

        /// <summary>
        /// Creates a new <see cref="Configuration"/>.
        /// </summary>
        public Configuration()
        {
            _configuration = _builder.Build();

            var sources = new ConfigurationSources(_builder.Sources, UpdateConfigurationRoot);

            Sources = sources;
        }

        internal void ChangeBasePath(string path)
        {
            this.SetBasePath(path);
            UpdateConfigurationRoot();
        }

        internal void ChangeFileProvider(IFileProvider fileProvider)
        {
            this.SetFileProvider(fileProvider);
            UpdateConfigurationRoot();
        }

        private void UpdateConfigurationRoot()
        {
            var current = _configuration;
            if (current is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _configuration = _builder.Build();
        }

        IConfigurationBuilder IConfigurationBuilder.Add(IConfigurationSource source)
        {
            Sources.Add(source);
            return this;
        }

        IConfigurationRoot IConfigurationBuilder.Build()
        {
            // No more modification is expected after this final build
            UpdateConfigurationRoot();
            return this;
        }

        IChangeToken IConfiguration.GetReloadToken()
        {
            // REVIEW: Is this correct?
            return _configuration.GetReloadToken();
        }

        void IConfigurationRoot.Reload()
        {
            _configuration.Reload();
        }

        // On source modifications, we rebuild configuration
        private class ConfigurationSources : IList<IConfigurationSource>
        {
            private readonly IList<IConfigurationSource> _sources;
            private readonly Action _sourcesModified;

            public ConfigurationSources(IList<IConfigurationSource> sources, Action sourcesModified)
            {
                _sources = sources;
                _sourcesModified = sourcesModified;
            }

            public IConfigurationSource this[int index]
            {
                get => _sources[index];
                set
                {
                    _sources[index] = value;
                    _sourcesModified();
                }
            }

            public int Count => _sources.Count;

            public bool IsReadOnly => _sources.IsReadOnly;

            public void Add(IConfigurationSource item)
            {
                _sources.Add(item);
                _sourcesModified();
            }

            public void Clear()
            {
                _sources.Clear();
                _sourcesModified();
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
                _sourcesModified();
            }

            public bool Remove(IConfigurationSource item)
            {
                var removed = _sources.Remove(item);
                _sourcesModified();
                return removed;
            }

            public void RemoveAt(int index)
            {
                _sources.RemoveAt(index);
                _sourcesModified();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}

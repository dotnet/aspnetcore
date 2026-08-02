// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

/// <summary>
/// WebAssemblyHostConfiguration is a class that implements the interface of an IConfiguration,
/// IConfigurationRoot, and IConfigurationBuilder. It can be used to simulatneously build
/// and read from a configuration object.
/// </summary>
public class WebAssemblyHostConfiguration : IConfiguration, IConfigurationRoot, IConfigurationBuilder
{
    private readonly List<IConfigurationProvider> _providers = new List<IConfigurationProvider>();
    private readonly List<IConfigurationSource> _sources = new List<IConfigurationSource>();

    private readonly List<IDisposable> _changeTokenRegistrations = new List<IDisposable>();
    private ConfigurationReloadToken _changeToken = new ConfigurationReloadToken();

    /// <summary>
    /// Gets the sources used to obtain configuration values.
    /// </summary>
    IList<IConfigurationSource> IConfigurationBuilder.Sources => new ReadOnlyCollection<IConfigurationSource>(_sources.ToArray());

    /// <summary>
    /// Gets the providers used to obtain configuration values.
    /// </summary>
    IEnumerable<IConfigurationProvider> IConfigurationRoot.Providers => new ReadOnlyCollection<IConfigurationProvider>(_providers.ToArray());

    /// <summary>
    /// Gets a key/value collection that can be used to share data between the <see cref="IConfigurationBuilder"/>
    /// and the registered <see cref="IConfigurationProvider"/> instances.
    /// </summary>
    // In this implementation, this largely exists as a way to satisfy the
    // requirements of the IConfigurationBuilder and is not populated by
    // the WebAssemblyHostConfiguration with any meaningful info.
    IDictionary<string, object> IConfigurationBuilder.Properties { get; } = new Dictionary<string, object>();

    /// <inheritdoc />
    public string? this[string key]
    {
        get
        {
            // Iterate through the providers in reverse to extract
            // the value from the most recently inserted provider.
            for (var i = _providers.Count - 1; i >= 0; i--)
            {
                var provider = _providers[i];

                if (provider.TryGet(key, out var value))
                {
                    return value;
                }
            }

            return null;
        }
        set
        {
            if (_providers.Count == 0)
            {
                throw new InvalidOperationException("Can only set property if at least one provider has been inserted.");
            }

            foreach (var provider in _providers)
            {
                provider.Set(key, value);
            }

        }
    }

    /// <summary>
    /// Gets a configuration sub-section with the specified key.
    /// </summary>
    /// <param name="key">The key of the configuration section.</param>
    /// <returns>The <see cref="IConfigurationSection"/>.</returns>
    /// <remarks>
    ///     This method will never return <c>null</c>. If no matching sub-section is found with the specified key,
    ///     an empty <see cref="IConfigurationSection"/> will be returned.
    /// </remarks>
    public IConfigurationSection GetSection(string key) => new ConfigurationSection(this, key);

    /// <summary>
    /// Gets the immediate descendant configuration sub-sections.
    /// </summary>
    /// <returns>The configuration sub-sections.</returns>
    IEnumerable<IConfigurationSection> IConfiguration.GetChildren()
    {
        var hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<IConfigurationSection>();
        foreach (var provider in _providers)
        {
            foreach (var child in provider.GetChildKeys(Enumerable.Empty<string>(), parentPath: null))
            {
                if (!hashSet.Add(child))
                {
                    continue;
                }

                result.Add(GetSection(child));
            }
        }

        return result;
    }

    /// <summary>
    /// Returns a <see cref="IChangeToken"/> that can be used to observe when this configuration is reloaded.
    /// </summary>
    /// <returns>The <see cref="IChangeToken"/>.</returns>
    public IChangeToken GetReloadToken() => _changeToken;

    /// <summary>
    /// Force the configuration values to be reloaded from the underlying sources.
    /// </summary>
    public void Reload()
    {
        foreach (var provider in _providers)
        {
            provider.Load();
        }
        RaiseChanged();
    }

    private void RaiseChanged()
    {
        var previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
        previousToken.OnReload();
    }

    /// <summary>
    /// Adds a new configuration source, retrieves the provider for the source, and
    /// adds a change listener that triggers a reload of the provider whenever a change
    /// is detected.
    /// </summary>
    /// <param name="source">The configuration source to add.</param>
    /// <returns>The same <see cref="IConfigurationBuilder"/>.</returns>
    public IConfigurationBuilder Add(IConfigurationSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        // Adds this source and its associated provider to the source
        // and provider references in this class. We make sure to load
        // the data from the provider so that values are properly initialized.
        _sources.Add(source);
        var provider = source.Build(this);
        provider.Load();

        // Add a handler that will detect when the the configuration
        // provider has reloaded data. This will invoke the RaiseChanged
        // method which maps changes in individual providers to the change
        // token on the WebAssemblyHostConfiguration object.
        _changeTokenRegistrations.Add(ChangeToken.OnChange(provider.GetReloadToken, RaiseChanged));

        // We keep a list of providers in this class so that we can map
        // set and get methods on this class to the set and get methods
        // on the individual configuration providers.
        _providers.Add(provider);
        return this;
    }

    /// <summary>
    /// Builds an <see cref="IConfiguration"/> with keys and values from the set of providers registered in
    /// <see cref="IConfigurationRoot.Providers"/>.
    /// </summary>
    /// <returns>An <see cref="IConfigurationRoot"/> with keys and values from the registered providers.</returns>
    public IConfigurationRoot Build()
    {
        return this;
    }

    /// <inheritdoc />
    public void Dispose()
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
}

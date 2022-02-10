// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using Microsoft.Extensions.Localization.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Localization;

/// <summary>
/// An <see cref="IStringLocalizer"/> that uses the <see cref="ResourceManager"/> and
/// <see cref="ResourceReader"/> to provide localized strings.
/// </summary>
/// <remarks>This type is thread-safe.</remarks>
public class ResourceManagerStringLocalizer : IStringLocalizer
{
    private readonly ConcurrentDictionary<string, object?> _missingManifestCache = new ConcurrentDictionary<string, object?>();
    private readonly IResourceNamesCache _resourceNamesCache;
    private readonly ResourceManager _resourceManager;
    private readonly IResourceStringProvider _resourceStringProvider;
    private readonly string _resourceBaseName;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new <see cref="ResourceManagerStringLocalizer"/>.
    /// </summary>
    /// <param name="resourceManager">The <see cref="ResourceManager"/> to read strings from.</param>
    /// <param name="resourceAssembly">The <see cref="Assembly"/> that contains the strings as embedded resources.</param>
    /// <param name="baseName">The base name of the embedded resource that contains the strings.</param>
    /// <param name="resourceNamesCache">Cache of the list of strings for a given resource assembly name.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    public ResourceManagerStringLocalizer(
        ResourceManager resourceManager,
        Assembly resourceAssembly,
        string baseName,
        IResourceNamesCache resourceNamesCache,
        ILogger logger)
        : this(
            resourceManager,
            new AssemblyWrapper(resourceAssembly),
            baseName,
            resourceNamesCache,
            logger)
    {
    }

    /// <summary>
    /// Intended for testing purposes only.
    /// </summary>
    internal ResourceManagerStringLocalizer(
        ResourceManager resourceManager,
        AssemblyWrapper resourceAssemblyWrapper,
        string baseName,
        IResourceNamesCache resourceNamesCache,
        ILogger logger)
        : this(
              resourceManager,
              new ResourceManagerStringProvider(
                  resourceNamesCache,
                  resourceManager,
                  resourceAssemblyWrapper.Assembly,
                  baseName),
              baseName,
              resourceNamesCache,
              logger)
    {
    }

    /// <summary>
    /// Intended for testing purposes only.
    /// </summary>
    internal ResourceManagerStringLocalizer(
        ResourceManager resourceManager,
        IResourceStringProvider resourceStringProvider,
        string baseName,
        IResourceNamesCache resourceNamesCache,
        ILogger logger)
    {
        if (resourceManager == null)
        {
            throw new ArgumentNullException(nameof(resourceManager));
        }

        if (resourceStringProvider == null)
        {
            throw new ArgumentNullException(nameof(resourceStringProvider));
        }

        if (baseName == null)
        {
            throw new ArgumentNullException(nameof(baseName));
        }

        if (resourceNamesCache == null)
        {
            throw new ArgumentNullException(nameof(resourceNamesCache));
        }

        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        _resourceStringProvider = resourceStringProvider;
        _resourceManager = resourceManager;
        _resourceBaseName = baseName;
        _resourceNamesCache = resourceNamesCache;
        _logger = logger;
    }

    /// <inheritdoc />
    public virtual LocalizedString this[string name]
    {
        get
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var value = GetStringSafely(name, null);

            return new LocalizedString(name, value ?? name, resourceNotFound: value == null, searchedLocation: _resourceBaseName);
        }
    }

    /// <inheritdoc />
    public virtual LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var format = GetStringSafely(name, null);
            var value = string.Format(CultureInfo.CurrentCulture, format ?? name, arguments);

            return new LocalizedString(name, value, resourceNotFound: format == null, searchedLocation: _resourceBaseName);
        }
    }

    /// <inheritdoc />
    public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        GetAllStrings(includeParentCultures, CultureInfo.CurrentUICulture);

    /// <summary>
    /// Returns all strings in the specified culture.
    /// </summary>
    /// <param name="includeParentCultures">Whether to include parent cultures in the search for a resource.</param>
    /// <param name="culture">The <see cref="CultureInfo"/> to get strings for.</param>
    /// <returns>The strings.</returns>
    protected IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures, CultureInfo culture)
    {
        if (culture == null)
        {
            throw new ArgumentNullException(nameof(culture));
        }

        var resourceNames = includeParentCultures
            ? GetResourceNamesFromCultureHierarchy(culture)
            : _resourceStringProvider.GetAllResourceStrings(culture, true);

        foreach (var name in resourceNames ?? Enumerable.Empty<string>())
        {
            var value = GetStringSafely(name, culture);
            yield return new LocalizedString(name, value ?? name, resourceNotFound: value == null, searchedLocation: _resourceBaseName);
        }
    }

    /// <summary>
    /// Gets a resource string from a <see cref="ResourceManager"/> and returns <c>null</c> instead of
    /// throwing exceptions if a match isn't found.
    /// </summary>
    /// <param name="name">The name of the string resource.</param>
    /// <param name="culture">The <see cref="CultureInfo"/> to get the string for.</param>
    /// <returns>The resource string, or <c>null</c> if none was found.</returns>
    protected string? GetStringSafely(string name, CultureInfo? culture)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        var keyCulture = culture ?? CultureInfo.CurrentUICulture;

        var cacheKey = $"name={name}&culture={keyCulture.Name}";

        _logger.SearchedLocation(name, _resourceBaseName, keyCulture);

        if (_missingManifestCache.ContainsKey(cacheKey))
        {
            return null;
        }

        try
        {
            return _resourceManager.GetString(name, culture);
        }
        catch (MissingManifestResourceException)
        {
            _missingManifestCache.TryAdd(cacheKey, null);
            return null;
        }
    }

    private IEnumerable<string> GetResourceNamesFromCultureHierarchy(CultureInfo startingCulture)
    {
        var currentCulture = startingCulture;
        var resourceNames = new HashSet<string>();

        var hasAnyCultures = false;

        while (true)
        {
            var cultureResourceNames = _resourceStringProvider.GetAllResourceStrings(currentCulture, false);

            if (cultureResourceNames != null)
            {
                foreach (var resourceName in cultureResourceNames)
                {
                    resourceNames.Add(resourceName);
                }
                hasAnyCultures = true;
            }

            if (currentCulture == currentCulture.Parent)
            {
                // currentCulture begat currentCulture, probably time to leave
                break;
            }

            currentCulture = currentCulture.Parent;
        }

        if (!hasAnyCultures)
        {
            throw new MissingManifestResourceException(Resources.Localization_MissingManifest_Parent);
        }

        return resourceNames;
    }
}

// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Microsoft.Extensions.Localization.Internal;

namespace Microsoft.Extensions.Localization
{
    /// <summary>
    /// An <see cref="IStringLocalizer"/> that uses the <see cref="System.Resources.ResourceManager"/> and
    /// <see cref="System.Resources.ResourceReader"/> to provide localized strings.
    /// </summary>
    /// <remarks>This type is thread-safe.</remarks>
    public class ResourceManagerStringLocalizer : IStringLocalizer
    {
        private readonly ConcurrentDictionary<string, object> _missingManifestCache = new ConcurrentDictionary<string, object>();
        private readonly IResourceNamesCache _resourceNamesCache;
        private readonly ResourceManager _resourceManager;
        private readonly AssemblyWrapper _resourceAssemblyWrapper;
        private readonly string _resourceBaseName;

        /// <summary>
        /// Creates a new <see cref="ResourceManagerStringLocalizer"/>.
        /// </summary>
        /// <param name="resourceManager">The <see cref="System.Resources.ResourceManager"/> to read strings from.</param>
        /// <param name="resourceAssembly">The <see cref="Assembly"/> that contains the strings as embedded resources.</param>
        /// <param name="baseName">The base name of the embedded resource in the <see cref="Assembly"/> that contains the strings.</param>
        /// <param name="resourceNamesCache">Cache of the list of strings for a given resource assembly name.</param>
        public ResourceManagerStringLocalizer(
            ResourceManager resourceManager,
            Assembly resourceAssembly,
            string baseName,
            IResourceNamesCache resourceNamesCache)
            : this(resourceManager, new AssemblyWrapper(resourceAssembly), baseName, resourceNamesCache)
        {
            if (resourceAssembly == null)
            {
                throw new ArgumentNullException(nameof(resourceAssembly));
            }
        }

        /// <summary>
        /// Intended for testing purposes only.
        /// </summary>
        public ResourceManagerStringLocalizer(
            ResourceManager resourceManager,
            AssemblyWrapper resourceAssemblyWrapper,
            string baseName,
            IResourceNamesCache resourceNamesCache)
        {
            if (resourceManager == null)
            {
                throw new ArgumentNullException(nameof(resourceManager));
            }

            if (resourceAssemblyWrapper == null)
            {
                throw new ArgumentNullException(nameof(resourceAssemblyWrapper));
            }

            if (baseName == null)
            {
                throw new ArgumentNullException(nameof(baseName));
            }

            if (resourceNamesCache == null)
            {
                throw new ArgumentNullException(nameof(resourceNamesCache));
            }

            _resourceAssemblyWrapper = resourceAssemblyWrapper;
            _resourceManager = resourceManager;
            _resourceBaseName = baseName;
            _resourceNamesCache = resourceNamesCache;
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
                return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
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
                var value = string.Format(format ?? name, arguments);
                return new LocalizedString(name, value, resourceNotFound: format == null);
            }
        }

        /// <summary>
        /// Creates a new <see cref="ResourceManagerStringLocalizer"/> for a specific <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="culture">The <see cref="CultureInfo"/> to use.</param>
        /// <returns>A culture-specific <see cref="ResourceManagerStringLocalizer"/>.</returns>
        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            return culture == null
                ? new ResourceManagerStringLocalizer(
                    _resourceManager,
                    _resourceAssemblyWrapper.Assembly,
                    _resourceBaseName,
                    _resourceNamesCache)
                : new ResourceManagerWithCultureStringLocalizer(
                    _resourceManager,
                    _resourceAssemblyWrapper.Assembly,
                    _resourceBaseName,
                    _resourceNamesCache,
                    culture);
        }

        /// <inheritdoc />
        public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            GetAllStrings(includeParentCultures, CultureInfo.CurrentUICulture);

        /// <summary>
        /// Returns all strings in the specified culture.
        /// </summary>
        /// <param name="includeParentCultures"></param>
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
                : GetResourceNamesForCulture(culture);

            if (resourceNames == null && !includeParentCultures)
            {
                var resourceStreamName = GetResourceStreamName(culture);
                throw new MissingManifestResourceException(
                    Resources.FormatLocalization_MissingManifest(resourceStreamName));
            }

            foreach (var name in resourceNames)
            {
                var value = GetStringSafely(name, culture);
                yield return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
            }
        }

        /// <summary>
        /// Gets a resource string from the <see cref="_resourceManager"/> and returns <c>null</c> instead of
        /// throwing exceptions if a match isn't found.
        /// </summary>
        /// <param name="name">The name of the string resource.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to get the string for.</param>
        /// <returns>The resource string, or <c>null</c> if none was found.</returns>
        protected string GetStringSafely(string name, CultureInfo culture)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var cacheKey = $"name={name}&culture={(culture ?? CultureInfo.CurrentUICulture).Name}";

            if (_missingManifestCache.ContainsKey(cacheKey))
            {
                return null;
            }

            try
            {
                return culture == null ? _resourceManager.GetString(name) : _resourceManager.GetString(name, culture);
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

                var cultureResourceNames = GetResourceNamesForCulture(currentCulture);

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

        private string GetResourceStreamName(CultureInfo culture)
        {
            var resourceStreamName = _resourceBaseName;
            if (!string.IsNullOrEmpty(culture.Name))
            {
                resourceStreamName += "." + culture.Name;
            }
            resourceStreamName += ".resources";

            return resourceStreamName;
        }

        private IList<string> GetResourceNamesForCulture(CultureInfo culture)
        {
            var resourceStreamName = GetResourceStreamName(culture);

            var cacheKey = $"assembly={_resourceAssemblyWrapper.FullName};resourceStreamName={resourceStreamName}";

            var cultureResourceNames = _resourceNamesCache.GetOrAdd(cacheKey, _ =>
            {
                using (var cultureResourceStream = _resourceAssemblyWrapper.GetManifestResourceStream(resourceStreamName))
                {
                    if (cultureResourceStream == null)
                    {
                        return null;
                    }

                    using (var resources = new ResourceReader(cultureResourceStream))
                    {
                        var names = new List<string>();
                        foreach (DictionaryEntry entry in resources)
                        {
                            var resourceName = (string)entry.Key;
                            names.Add(resourceName);
                        }
                        return names;
                    }
                }

            });

            return cultureResourceNames;
        }
    }
}
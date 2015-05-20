// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Localization.Internal;

namespace Microsoft.Framework.Localization
{
    /// <summary>
    /// An <see cref="IStringLocalizer"/> that uses the <see cref="System.Resources.ResourceManager"/> and
    /// <see cref="System.Resources.ResourceReader"/> to provide localized strings.
    /// </summary>
    public class ResourceManagerStringLocalizer : IStringLocalizer
    {
        private static readonly ConcurrentDictionary<string, IList<string>> _resourceNamesCache =
            new ConcurrentDictionary<string, IList<string>>();

        private readonly ConcurrentDictionary<string, object> _missingManifestCache =
            new ConcurrentDictionary<string, object>();

        private readonly ResourceManager _resourceManager;
        private readonly AssemblyWrapper _resourceAssemblyWrapper;
        private readonly string _resourceBaseName;

        /// <summary>
        /// Creates a new <see cref="ResourceManagerStringLocalizer"/>.
        /// </summary>
        /// <param name="resourceManager">The <see cref="System.Resources.ResourceManager"/> to read strings from.</param>
        /// <param name="resourceAssembly">The <see cref="Assembly"/> that contains the strings as embedded resources.</param>
        /// <param name="baseName">The base name of the embedded resource in the <see cref="Assembly"/> that contains the strings.</param>
        public ResourceManagerStringLocalizer(
            [NotNull] ResourceManager resourceManager,
            [NotNull] Assembly resourceAssembly,
            [NotNull] string baseName)
            : this(resourceManager, new AssemblyWrapper(resourceAssembly), baseName)
        {
            
        }

        /// <summary>
        /// Intended for testing purposes only.
        /// </summary>
        public ResourceManagerStringLocalizer(
            [NotNull] ResourceManager resourceManager,
            [NotNull] AssemblyWrapper resourceAssemblyWrapper,
            [NotNull] string baseName)
        {
            _resourceAssemblyWrapper = resourceAssemblyWrapper;
            _resourceManager = resourceManager;
            _resourceBaseName = baseName;
        }

        /// <inheritdoc />
        public virtual LocalizedString this[[NotNull] string name]
        {
            get
            {
                var value = GetStringSafely(name, null);
                return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
            }
        }

        /// <inheritdoc />
        public virtual LocalizedString this[[NotNull] string name, params object[] arguments]
        {
            get
            {
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
                ? new ResourceManagerStringLocalizer(_resourceManager, _resourceAssemblyWrapper, _resourceBaseName)
                : new ResourceManagerWithCultureStringLocalizer(_resourceManager,
                    _resourceAssemblyWrapper,
                    _resourceBaseName,
                    culture);
        }

        /// <summary>
        /// Gets a resource string from the <see cref="_resourceManager"/> and returns <c>null</c> instead of
        /// throwing exceptions if a match isn't found.
        /// </summary>
        /// <param name="name">The name of the string resource.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to get the string for.</param>
        /// <returns>The resource string, or <c>null</c> if none was found.</returns>
        protected string GetStringSafely([NotNull] string name, CultureInfo culture)
        {
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

        /// <summary>
        /// Returns an <see cref="IEnumerator{LocalizedString}"/> for all strings in the current culture.
        /// </summary>
        /// <returns>The <see cref="IEnumerator{LocalizedString}"/>.</returns>
        public virtual IEnumerator<LocalizedString> GetEnumerator()
        {
            return GetEnumerator(CultureInfo.CurrentUICulture);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator{LocalizedString}"/> for all strings in the specified culture.
        /// </summary>
        /// <param name="culture">The <see cref="CultureInfo"/> to get strings for.</param>
        /// <returns>The <see cref="IEnumerator{LocalizedString}"/>.</returns>
        protected IEnumerator<LocalizedString> GetEnumerator([NotNull] CultureInfo culture)
        {
            var resourceNames = GetResourceNamesFromCultureHierarchy(culture);

            foreach (var name in resourceNames)
            {
                var value = GetStringSafely(name, culture);
                yield return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
            }
        }

        // Internal to allow testing
        internal static void ClearResourceNamesCache() => _resourceNamesCache.Clear();

        private IEnumerable<string> GetResourceNamesFromCultureHierarchy(CultureInfo startingCulture)
        {
            var currentCulture = startingCulture;
            var resourceNames = new HashSet<string>();

            while (true)
            {
                try
                {
                    var cultureResourceNames = GetResourceNamesForCulture(currentCulture);
                    foreach (var resourceName in cultureResourceNames)
                    {
                        resourceNames.Add(resourceName);
                    }
                }
                catch (MissingManifestResourceException) { }

                if (currentCulture == currentCulture.Parent)
                {
                    // currentCulture begat currentCulture, probably time to leave
                    break;
                }

                currentCulture = currentCulture.Parent;
            }

            return resourceNames;
        }

        private IList<string> GetResourceNamesForCulture(CultureInfo culture)
        {
            var resourceStreamName = _resourceBaseName;
            if (!string.IsNullOrEmpty(culture.Name))
            {
                resourceStreamName += "." + culture.Name;
            }
            resourceStreamName += ".resources";

            var cacheKey = $"assembly={_resourceAssemblyWrapper.FullName};resourceStreamName={resourceStreamName}";

            var cultureResourceNames = _resourceNamesCache.GetOrAdd(cacheKey, key =>
            {
                var names = new List<string>();
                using (var cultureResourceStream = _resourceAssemblyWrapper.GetManifestResourceStream(key))
                using (var resources = new ResourceReader(cultureResourceStream))
                {
                    foreach (DictionaryEntry entry in resources)
                    {
                        var resourceName = (string)entry.Key;
                        names.Add(resourceName);
                    }
                }

                return names;
            });

            return cultureResourceNames;
        }
    }
}
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

namespace Microsoft.Framework.Localization
{
    /// <summary>
    /// An <see cref="IStringLocalizer"/> that uses the <see cref="System.Resources.ResourceManager"/> and
    /// <see cref="System.Resources.ResourceReader"/> to provide localized strings.
    /// </summary>
    public class ResourceManagerStringLocalizer : IStringLocalizer
    {
        private readonly ConcurrentDictionary<MissingManifestCacheKey, object> _missingManifestCache =
            new ConcurrentDictionary<MissingManifestCacheKey, object>();

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
        {
            ResourceManager = resourceManager;
            ResourceAssembly = resourceAssembly;
            ResourceBaseName = baseName;
        }

        /// <summary>
        /// The <see cref="System.Resources.ResourceManager"/> to read strings from.
        /// </summary>
        protected ResourceManager ResourceManager { get; }

        /// <summary>
        /// The <see cref="Assembly"/> that contains the strings as embedded resources.
        /// </summary>
        protected Assembly ResourceAssembly { get; }

        /// <summary>
        /// The base name of the embedded resource in the <see cref="Assembly"/> that contains the strings.
        /// </summary>
        protected string ResourceBaseName { get; }
        
        /// <inheritdoc />
        public virtual LocalizedString this[[NotNull] string name] => GetString(name);

        /// <inheritdoc />
        public virtual LocalizedString this[[NotNull] string name, params object[] arguments] => GetString(name, arguments);

        /// <inheritdoc />
        public virtual LocalizedString GetString([NotNull] string name)
        {
            var value = GetStringSafely(name, null);
            return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
        }

        /// <inheritdoc />
        public virtual LocalizedString GetString([NotNull] string name, params object[] arguments)
        {
            var format = GetStringSafely(name, null);
            var value = string.Format(format ?? name, arguments);
            return new LocalizedString(name, value, resourceNotFound: format == null);
        }

        /// <summary>
        /// Creates a new <see cref="ResourceManagerStringLocalizer"/> for a specific <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="culture">The <see cref="CultureInfo"/> to use.</param>
        /// <returns>A culture-specific <see cref="ResourceManagerStringLocalizer"/>.</returns>
        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            return culture == null
                ? new ResourceManagerStringLocalizer(ResourceManager, ResourceAssembly, ResourceBaseName)
                : new ResourceManagerWithCultureStringLocalizer(
                    ResourceManager,
                    ResourceAssembly,
                    ResourceBaseName,
                    culture);
        }

        /// <summary>
        /// Gets a resource string from the <see cref="ResourceManager"/> and returns <c>null</c> instead of
        /// throwing exceptions if a match isn't found.
        /// </summary>
        /// <param name="name">The name of the string resource.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to get the string for.</param>
        /// <returns>The resource string, or <c>null</c> if none was found.</returns>
        protected string GetStringSafely([NotNull] string name, [NotNull] CultureInfo culture)
        {
            var cacheKey = new MissingManifestCacheKey(name, culture);
            if (_missingManifestCache.ContainsKey(cacheKey))
            {
                return null;
            }

            try
            {
                return culture == null ? ResourceManager.GetString(name) : ResourceManager.GetString(name, culture);
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
        public virtual IEnumerator<LocalizedString> GetEnumerator() => GetEnumerator(CultureInfo.CurrentUICulture);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Returns an <see cref="IEnumerator{LocalizedString}"/> for all strings in the specified culture.
        /// </summary>
        /// <param name="culture">The <see cref="CultureInfo"/> to get strings for.</param>
        /// <returns>The <see cref="IEnumerator{LocalizedString}"/>.</returns>
        protected IEnumerator<LocalizedString> GetEnumerator([NotNull] CultureInfo culture)
        {
            // TODO: I'm sure something here should be cached, probably the whole result
            var resourceNames = GetResourceNamesFromCultureHierarchy(culture);

            foreach (var name in resourceNames)
            {
                var value = GetStringSafely(name, culture);
                yield return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
            }
        }

        private IEnumerable<string> GetResourceNamesFromCultureHierarchy(CultureInfo startingCulture)
        {
            var currentCulture = startingCulture;
            var resourceNames = new HashSet<string>();

            while (true)
            {
                try
                {
                    var resourceStreamName = ResourceBaseName;
                    if (!string.IsNullOrEmpty(currentCulture.Name))
                    {
                        resourceStreamName += "." + currentCulture.Name;
                    }
                    resourceStreamName += ".resources";
                    using (var cultureResourceStream = ResourceAssembly.GetManifestResourceStream(resourceStreamName))
                    using (var resources = new ResourceReader(cultureResourceStream))
                    {
                        foreach (DictionaryEntry entry in resources)
                        {
                            var resourceName = (string)entry.Key;
                            resourceNames.Add(resourceName);
                        }
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

        private class MissingManifestCacheKey : IEquatable<MissingManifestCacheKey>
        {
            private readonly int _hashCode;

            public MissingManifestCacheKey(string name, CultureInfo culture)
            {
                Name = name;
                CultureInfo = culture;
                _hashCode = new { Name, CultureInfo }.GetHashCode();
            }

            public string Name { get; }

            public CultureInfo CultureInfo { get; }

            public bool Equals(MissingManifestCacheKey other) =>
                string.Equals(Name, other.Name, StringComparison.Ordinal) && CultureInfo == other.CultureInfo;

            public override bool Equals(object obj)
            {
                var other = obj as MissingManifestCacheKey;

                if (other != null)
                {
                    return Equals(other);
                }

                return base.Equals(obj);
            }

            public override int GetHashCode() => _hashCode;
        }
    }
}
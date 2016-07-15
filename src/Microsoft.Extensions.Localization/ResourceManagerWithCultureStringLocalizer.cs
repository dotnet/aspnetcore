// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Microsoft.Extensions.Localization.Internal;

namespace Microsoft.Extensions.Localization
{
    /// <summary>
    /// An <see cref="IStringLocalizer"/> that uses the <see cref="ResourceManager"/> and
    /// <see cref="ResourceReader"/> to provide localized strings for a specific <see cref="CultureInfo"/>.
    /// </summary>
    public class ResourceManagerWithCultureStringLocalizer : ResourceManagerStringLocalizer
    {
        private readonly CultureInfo _culture;

        /// <summary>
        /// Creates a new <see cref="ResourceManagerWithCultureStringLocalizer"/>.
        /// </summary>
        /// <param name="resourceManager">The <see cref="ResourceManager"/> to read strings from.</param>
        /// <param name="resourceStringProvider">The <see cref="IResourceStringProvider"/> that can find the resources.</param>
        /// <param name="baseName">The base name of the embedded resource that contains the strings.</param>
        /// <param name="resourceNamesCache">Cache of the list of strings for a given resource assembly name.</param>
        /// <param name="culture">The specific <see cref="CultureInfo"/> to use.</param>
        internal ResourceManagerWithCultureStringLocalizer(
            ResourceManager resourceManager,
            IResourceStringProvider resourceStringProvider,
            string baseName,
            IResourceNamesCache resourceNamesCache,
            CultureInfo culture)
            : base(resourceManager, resourceStringProvider, baseName, resourceNamesCache)
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

            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            _culture = culture;
        }

        /// <summary>
        /// Creates a new <see cref="ResourceManagerWithCultureStringLocalizer"/>.
        /// </summary>
        /// <param name="resourceManager">The <see cref="ResourceManager"/> to read strings from.</param>
        /// <param name="resourceAssembly">The <see cref="Assembly"/> that contains the strings as embedded resources.</param>
        /// <param name="baseName">The base name of the embedded resource that contains the strings.</param>
        /// <param name="resourceNamesCache">Cache of the list of strings for a given resource assembly name.</param>
        /// <param name="culture">The specific <see cref="CultureInfo"/> to use.</param>
        public ResourceManagerWithCultureStringLocalizer(
            ResourceManager resourceManager,
            Assembly resourceAssembly,
            string baseName,
            IResourceNamesCache resourceNamesCache,
            CultureInfo culture)
            : base(resourceManager, resourceAssembly, baseName, resourceNamesCache)
        {
            if (resourceManager == null)
            {
                throw new ArgumentNullException(nameof(resourceManager));
            }

            if (resourceAssembly == null)
            {
                throw new ArgumentNullException(nameof(resourceAssembly));
            }

            if (baseName == null)
            {
                throw new ArgumentNullException(nameof(baseName));
            }

            if (resourceNamesCache == null)
            {
                throw new ArgumentNullException(nameof(resourceNamesCache));
            }

            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            _culture = culture;
        }

        /// <inheritdoc />
        public override LocalizedString this[string name]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                var value = GetStringSafely(name, _culture);
                return new LocalizedString(name, value ?? name);
            }
        }

        /// <inheritdoc />
        public override LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                var format = GetStringSafely(name, _culture);
                var value = string.Format(_culture, format ?? name, arguments);
                return new LocalizedString(name, value ?? name, resourceNotFound: format == null);
            }
        }

        /// <inheritdoc />
        public override IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            GetAllStrings(includeParentCultures, _culture);
    }
}
// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

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
    /// <see cref="System.Resources.ResourceReader"/> to provide localized strings for a specific <see cref="CultureInfo"/>.
    /// </summary>
    public class ResourceManagerWithCultureStringLocalizer : ResourceManagerStringLocalizer
    {
        private readonly CultureInfo _culture;

        /// <summary>
        /// Creates a new <see cref="ResourceManagerWithCultureStringLocalizer"/>.
        /// </summary>
        /// <param name="resourceManager">The <see cref="System.Resources.ResourceManager"/> to read strings from.</param>
        /// <param name="resourceAssembly">The <see cref="Assembly"/> that contains the strings as embedded resources.</param>
        /// <param name="baseName">The base name of the embedded resource in the <see cref="Assembly"/> that contains the strings.</param>
        /// <param name="culture">The specific <see cref="CultureInfo"/> to use.</param>
        public ResourceManagerWithCultureStringLocalizer(
            [NotNull] ResourceManager resourceManager,
            [NotNull] Assembly assembly,
            [NotNull] string baseName,
            [NotNull] CultureInfo culture)
            : base(resourceManager, assembly, baseName)
        {
            _culture = culture;
        }

        /// <summary>
        /// Intended for testing purposes only.
        /// </summary>
        public ResourceManagerWithCultureStringLocalizer(
            [NotNull] ResourceManager resourceManager,
            [NotNull] AssemblyWrapper assemblyWrapper,
            [NotNull] string baseName,
            [NotNull] CultureInfo culture)
            : base(resourceManager, assemblyWrapper, baseName)
        {
            _culture = culture;
        }

        /// <inheritdoc />
        public override LocalizedString this[[NotNull] string name]
        {
            get
            {
                var value = GetStringSafely(name, _culture);
                return new LocalizedString(name, value ?? name);
            }
        }

        /// <inheritdoc />
        public override LocalizedString this[[NotNull] string name, params object[] arguments]
        {
            get
            {
                var format = GetStringSafely(name, _culture);
                var value = string.Format(_culture, format ?? name, arguments);
                return new LocalizedString(name, value ?? name, resourceNotFound: format == null);
            }
        }

        /// <inheritdoc />
        public override IEnumerator<LocalizedString> GetEnumerator()
        {
            return GetEnumerator(_culture);
        }
    }
}
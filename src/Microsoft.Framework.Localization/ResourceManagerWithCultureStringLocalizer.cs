// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Microsoft.Framework.Internal;

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

        /// <inheritdoc />
        public override LocalizedString this[[NotNull] string name] => GetString(name);

        /// <inheritdoc />
        public override LocalizedString this[[NotNull] string name, params object[] arguments] => GetString(name, arguments);

        /// <inheritdoc />
        public override LocalizedString GetString([NotNull] string name)
        {
            var value = GetStringSafely(name, _culture);
            return new LocalizedString(name, value ?? name);
        }

        /// <inheritdoc />
        public override LocalizedString GetString([NotNull] string name, params object[] arguments)
        {
            var format = GetStringSafely(name, _culture);
            var value = string.Format(_culture, format ?? name, arguments);
            return new LocalizedString(name, value ?? name, resourceNotFound: format == null);
        }

        /// <inheritdoc />
        public override IEnumerator<LocalizedString> GetEnumerator() => GetEnumerator(_culture);
    }
}
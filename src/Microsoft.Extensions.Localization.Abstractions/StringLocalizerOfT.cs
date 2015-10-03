// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Localization
{
    /// <summary>
    /// Provides strings for <see cref="TResourceSource"/>.
    /// </summary>
    /// <typeparam name="TResourceSource">The <see cref="System.Type"/> to provide strings for.</typeparam>
    public class StringLocalizer<TResourceSource> : IStringLocalizer<TResourceSource>
    {
        private IStringLocalizer _localizer;

        /// <summary>
        /// Creates a new <see cref="StringLocalizer{TResourceSource}"/>.
        /// </summary>
        /// <param name="factory">The <see cref="IStringLocalizerFactory"/> to use.</param>
        public StringLocalizer([NotNull] IStringLocalizerFactory factory)
        {
            _localizer = factory.Create(typeof(TResourceSource));
        }

        /// <inheritdoc />
        public virtual IStringLocalizer WithCulture(CultureInfo culture) => _localizer.WithCulture(culture);

        /// <inheritdoc />
        public virtual LocalizedString this[[NotNull] string name] => _localizer[name];

        /// <inheritdoc />
        public virtual LocalizedString this[[NotNull] string name, params object[] arguments] =>
            _localizer[name, arguments];

        /// <inheritdoc />
        public IEnumerable<LocalizedString> GetAllStrings(bool includeAncestorCultures) =>
            _localizer.GetAllStrings(includeAncestorCultures);
    }
}
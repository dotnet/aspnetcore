// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Framework.Localization
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
        public StringLocalizer(IStringLocalizerFactory factory)
        {
            _localizer = factory.Create(typeof(TResourceSource));
        }

        /// <inheritdoc />
        public virtual IStringLocalizer WithCulture(CultureInfo culture) => _localizer.WithCulture(culture);

        /// <inheritdoc />
        public virtual LocalizedString this[string key] => _localizer[key];

        /// <inheritdoc />
        public virtual LocalizedString this[string key, params object[] arguments] => _localizer[key, arguments];

        /// <inheritdoc />
        public virtual LocalizedString GetString(string key) => _localizer.GetString(key);

        /// <inheritdoc />
        public virtual LocalizedString GetString(string key, params object[] arguments) =>
            _localizer.GetString(key, arguments);

        /// <inheritdoc />
        public IEnumerator<LocalizedString> GetEnumerator() => _localizer.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _localizer.GetEnumerator();
    }
}
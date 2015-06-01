// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Localization;

namespace Microsoft.AspNet.Mvc.Localization
{
    /// <summary>
    /// This is an <see cref="HtmlLocalizer"/> that provides localized HTML content.
    /// </summary>
    /// <typeparam name = "TResource"> The <see cref="System.Type"/> to scope the resource names.</typeparam>
    public class HtmlLocalizer<TResource> : IHtmlLocalizer<TResource>
    {
        private readonly IHtmlLocalizer _localizer;

        /// <summary>
        /// Creates a new <see cref="HtmlLocalizer"/>.
        /// </summary>
        /// <param name="factory">The <see cref="IHtmlLocalizerFactory"/>.</param>
        public HtmlLocalizer(IHtmlLocalizerFactory factory)
        {
            _localizer = factory.Create(typeof(TResource));
        }

        /// <inheritdoc />
        public virtual LocalizedString this[[NotNull] string key] => _localizer[key];

        /// <inheritdoc />
        public virtual LocalizedString this[[NotNull] string key, params object[] arguments] =>
            _localizer[key, arguments];

        /// <inheritdoc />
        public virtual IHtmlLocalizer WithCulture([NotNull] CultureInfo culture) => _localizer.WithCulture(culture);

        /// <inheritdoc />
        IStringLocalizer IStringLocalizer.WithCulture([NotNull] CultureInfo culture) =>
            _localizer.WithCulture(culture);

        /// <inheritdoc />
        public virtual LocalizedString GetString([NotNull] string key) => _localizer.GetString(key);

        /// <inheritdoc />
        public virtual LocalizedString GetString([NotNull] string key, params object[] arguments) =>
            _localizer.GetString(key, arguments);

        /// <inheritdoc />
        public virtual LocalizedHtmlString Html([NotNull] string key) => _localizer.Html(key);

        /// <inheritdoc />
        public virtual LocalizedHtmlString Html([NotNull] string key, params object[] arguments) =>
            _localizer.Html(key, arguments);

        /// <inheritdoc />
        public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeAncestorCultures) =>
            _localizer.GetAllStrings(includeAncestorCultures);
    }
}
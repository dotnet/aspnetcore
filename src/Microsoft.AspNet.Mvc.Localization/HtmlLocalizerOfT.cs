// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
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
        public virtual LocalizedString this[string key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                return _localizer[key];
            }
        }

        /// <inheritdoc />
        public virtual LocalizedString this[string key, params object[] arguments]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                return _localizer[key, arguments];
            }
        }

        /// <inheritdoc />
        public virtual IHtmlLocalizer WithCulture(CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            return _localizer.WithCulture(culture);
        }

        /// <inheritdoc />
        IStringLocalizer IStringLocalizer.WithCulture(CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            return _localizer.WithCulture(culture);
        }

        /// <inheritdoc />
        public virtual LocalizedString GetString(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _localizer.GetString(key);
        }

        /// <inheritdoc />
        public virtual LocalizedString GetString(string key, params object[] arguments)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _localizer.GetString(key, arguments);
        }

        /// <inheritdoc />
        public virtual LocalizedHtmlString Html(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _localizer.Html(key);
        }

        /// <inheritdoc />
        public virtual LocalizedHtmlString Html(string key, params object[] arguments)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _localizer.Html(key, arguments);
        }

        /// <inheritdoc />
        public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeAncestorCultures) =>
            _localizer.GetAllStrings(includeAncestorCultures);
    }
}
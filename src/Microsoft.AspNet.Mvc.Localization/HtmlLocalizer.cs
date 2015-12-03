// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNet.Mvc.Localization
{
    /// <summary>
    /// An <see cref="IHtmlLocalizer"/> that uses the <see cref="IStringLocalizer"/> to provide localized HTML content.
    /// This service just encodes the arguments but not the resource string.
    /// </summary>
    public class HtmlLocalizer : IHtmlLocalizer
    {
        private IStringLocalizer _localizer;

        /// <summary>
        /// Creates a new <see cref="HtmlLocalizer"/>.
        /// </summary>
        /// <param name="localizer">The <see cref="IStringLocalizer"/> to read strings from.</param>
        public HtmlLocalizer(IStringLocalizer localizer)
        {
            if (localizer == null)
            {
                throw new ArgumentNullException(nameof(localizer));
            }

            _localizer = localizer;
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

        /// <summary>
        /// Creates a new <see cref="IHtmlLocalizer"/> for a specific <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="culture">The <see cref="CultureInfo"/> to use.</param>
        /// <returns>A culture-specific <see cref="IHtmlLocalizer"/>.</returns>
        public virtual IHtmlLocalizer WithCulture(CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            return new HtmlLocalizer(_localizer.WithCulture(culture));
        }

        /// <summary>
        /// Creates a new <see cref="IStringLocalizer"/> for a specific <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="culture">The <see cref="CultureInfo"/> to use.</param>
        /// <returns>A culture-specific <see cref="IStringLocalizer"/>.</returns>
        IStringLocalizer IStringLocalizer.WithCulture(CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            return new HtmlLocalizer(_localizer.WithCulture(culture));
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
        public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeAncestorCultures) =>
            _localizer.GetAllStrings(includeAncestorCultures);

        /// <inheritdoc />
        public virtual LocalizedHtmlString Html(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return ToHtmlString(_localizer.GetString(key));
        }

        /// <inheritdoc />
        public virtual LocalizedHtmlString Html(string key, params object[] arguments)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return ToHtmlString(_localizer.GetString(key), arguments);
        }

        /// <summary>
        /// Creates a new <see cref="LocalizedHtmlString"/> for a <see cref="LocalizedString"/>.
        /// </summary>
        /// <param name="result">The <see cref="LocalizedString"/>.</param>
        protected virtual LocalizedHtmlString ToHtmlString(LocalizedString result) =>
            new LocalizedHtmlString(result.Name, result.Value, result.ResourceNotFound);

        protected virtual LocalizedHtmlString ToHtmlString(LocalizedString result, object[] arguments) =>
            new LocalizedHtmlString(result.Name, result.Value, result.ResourceNotFound, arguments);
    }
}
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.Localization
{
    /// <summary>
    /// An <see cref="IHtmlLocalizer"/> implementation that provides localized HTML content for the specified type
    /// <typeparamref name="TResource"/>.
    /// </summary>
    /// <typeparam name="TResource">The <see cref="Type"/> to scope the resource names.</typeparam>
    public class HtmlLocalizer<TResource> : IHtmlLocalizer<TResource>
    {
        private readonly IHtmlLocalizer _localizer;

        /// <summary>
        /// Creates a new <see cref="HtmlLocalizer{TResource}"/>.
        /// </summary>
        /// <param name="factory">The <see cref="IHtmlLocalizerFactory"/>.</param>
        public HtmlLocalizer(IHtmlLocalizerFactory factory)
        {
            _localizer = factory.Create(typeof(TResource));
        }

        /// <inheritdoc />
        public virtual LocalizedHtmlString this[string name]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                return _localizer[name];
            }
        }

        /// <inheritdoc />
        public virtual LocalizedHtmlString this[string name, params object[] arguments]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                return _localizer[name, arguments];
            }
        }

        /// <inheritdoc />
        public virtual LocalizedString GetString(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return _localizer.GetString(name);
        }

        /// <inheritdoc />
        public virtual LocalizedString GetString(string name, params object[] arguments)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return _localizer.GetString(name, arguments);
        }

        /// <inheritdoc />
        public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            _localizer.GetAllStrings(includeParentCultures);
    }
}

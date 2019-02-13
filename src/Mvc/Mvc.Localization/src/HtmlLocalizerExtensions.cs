// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.Localization
{
    /// <summary>
    /// Extension methods for <see cref="IHtmlLocalizer"/>.
    /// </summary>
    public static class HtmlLocalizerExtensions
    {
        /// <summary>
        /// Gets the <see cref="LocalizedHtmlString"/> resource for a specific name.
        /// </summary>
        /// <param name="htmlLocalizer">The <see cref="IHtmlLocalizer"/>.</param>
        /// <param name="name">The key to use.</param>
        /// <returns>The <see cref="LocalizedHtmlString"/> resource.</returns>
        public static LocalizedHtmlString GetHtml(this IHtmlLocalizer htmlLocalizer, string name)
        {
            if (htmlLocalizer == null)
            {
                throw new ArgumentNullException(nameof(htmlLocalizer));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return htmlLocalizer[name];
        }

        /// <summary>
        /// Gets the <see cref="LocalizedHtmlString"/> resource for a specific name.
        /// </summary>
        /// <param name="htmlLocalizer">The <see cref="IHtmlLocalizer"/>.</param>
        /// <param name="name">The key to use.</param>
        /// <param name="arguments">The values to format the string with.</param>
        /// <returns>The <see cref="LocalizedHtmlString"/> resource.</returns>
        public static LocalizedHtmlString GetHtml(this IHtmlLocalizer htmlLocalizer, string name, params object[] arguments)
        {
            if (htmlLocalizer == null)
            {
                throw new ArgumentNullException(nameof(htmlLocalizer));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return htmlLocalizer[name, arguments];
        }

        /// <summary>
        /// Gets all string resources including those for parent cultures.
        /// </summary>
        /// <param name="htmlLocalizer">The <see cref="IHtmlLocalizer"/>.</param>
        /// <returns>The string resources.</returns>
        public static IEnumerable<LocalizedString> GetAllStrings(this IHtmlLocalizer htmlLocalizer)
        {
            if (htmlLocalizer == null)
            {
                throw new ArgumentNullException(nameof(htmlLocalizer));
            }

            return htmlLocalizer.GetAllStrings(includeParentCultures: true);
        }
    }
}

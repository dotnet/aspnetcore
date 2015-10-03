// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Collections.Generic;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Localization
{
    public static class StringLocalizerExtensions
    {
        /// <summary>
        /// Gets the string resource with the given name.
        /// </summary>
        /// <param name="stringLocalizer">The <see cref="IStringLocalizer"/>.</param>
        /// <param name="name">The name of the string resource.</param>
        /// <returns>The string resource as a <see cref="LocalizedString"/>.</returns>
        public static LocalizedString GetString(
            [NotNull] this IStringLocalizer stringLocalizer,
            [NotNull] string name) => stringLocalizer[name];

        /// <summary>
        /// Gets the string resource with the given name and formatted with the supplied arguments.
        /// </summary>
        /// <param name="stringLocalizer">The <see cref="IStringLocalizer"/>.</param>
        /// <param name="name">The name of the string resource.</param>
        /// <param name="arguments">The values to format the string with.</param>
        /// <returns>The formatted string resource as a <see cref="LocalizedString"/>.</returns>
        public static LocalizedString GetString(
            [NotNull] this IStringLocalizer stringLocalizer,
            [NotNull] string name,
            params object[] arguments) => stringLocalizer[name, arguments];

        /// <summary>
        /// Gets all string resources including those for ancestor cultures.
        /// </summary>
        /// <param name="stringLocalizer">The <see cref="IStringLocalizer"/>.</param>
        /// <returns>The string resources.</returns>
        public static IEnumerable<LocalizedString> GetAllStrings([NotNull] this IStringLocalizer stringLocalizer) =>
            stringLocalizer.GetAllStrings(includeAncestorCultures: true);
    }
}

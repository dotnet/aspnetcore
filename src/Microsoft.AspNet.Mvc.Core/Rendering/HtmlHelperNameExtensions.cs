// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Name-related extensions for <see cref="HtmlHelper"/> and <see cref="HtmlHelper{T}"/>.
    /// </summary>
    public static class HtmlHelperNameExtensions
    {
        /// <summary>
        /// Gets the full HTML field name for the current model.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="HtmlHelper"/> instance that this method extends.</param>
        /// <returns>An <see cref="HtmlString"/> that represents HTML markup.</returns>
        public static HtmlString NameForModel([NotNull] this IHtmlHelper htmlHelper)
        {
            return htmlHelper.Name(string.Empty);
        }

        /// <summary>
        /// Gets the full HTML field id for the current model.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="HtmlHelper"/> instance that this method extends.</param>
        /// <returns>An <see cref="HtmlString"/> that represents HTML markup.</returns>
        public static HtmlString IdForModel([NotNull] this IHtmlHelper htmlHelper)
        {
            return htmlHelper.Id(string.Empty);
        }
    }
}

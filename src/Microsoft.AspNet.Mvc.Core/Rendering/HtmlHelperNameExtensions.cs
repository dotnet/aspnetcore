// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Name-related extensions for <see cref="IHtmlHelper"/>.
    /// </summary>
    public static class HtmlHelperNameExtensions
    {
        /// <summary>
        /// Returns the full HTML element name for the current model.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <returns>A <see cref="string"/> containing the element name.</returns>
        public static string NameForModel([NotNull] this IHtmlHelper htmlHelper)
        {
            return htmlHelper.Name(expression: null);
        }

        /// <summary>
        /// Returns the HTML element Id for the current model.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <returns>A <see cref="string"/> containing the element Id.</returns>
        public static string IdForModel([NotNull] this IHtmlHelper htmlHelper)
        {
            return htmlHelper.Id(expression: null);
        }
    }
}

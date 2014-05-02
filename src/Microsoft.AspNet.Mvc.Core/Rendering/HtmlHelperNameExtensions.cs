// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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

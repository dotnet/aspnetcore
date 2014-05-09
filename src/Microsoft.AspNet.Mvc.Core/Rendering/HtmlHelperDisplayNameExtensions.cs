// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// DisplayName-related extensions for <see cref="HtmlHelper"/> and <see cref="IHtmlHelper{T}"/>.
    /// </summary>
    public static class HtmlHelperDisplayNameExtensions
    {
        /// <summary>
        /// Gets the display name for the current model.
        /// </summary>
        /// <returns>An <see cref="HtmlString"/> that represents HTML markup.</returns>
        public static HtmlString DisplayNameForModel([NotNull] this IHtmlHelper htmlHelper)
        {
            return htmlHelper.DisplayName(string.Empty);
        }

        /// <summary>
        /// Gets the display name for the model.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{T}"/> instance that this method extends.</param>
        /// <param name="expression">An expression that identifies the object that contains the display name.</param>
        /// <returns>
        /// The display name for the model.
        /// </returns>
        public static HtmlString DisplayNameFor<TInnerModel,TValue>(
            [NotNull] this IHtmlHelper<IEnumerable<TInnerModel>> htmlHelper,
            [NotNull] Expression<Func<TInnerModel, TValue>> expression)
        {
            return htmlHelper.DisplayNameForInnerType<TInnerModel, TValue>(expression);
        }
    }
}

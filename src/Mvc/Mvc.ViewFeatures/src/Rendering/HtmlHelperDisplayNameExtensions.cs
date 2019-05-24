// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    /// <summary>
    /// DisplayName-related extensions for <see cref="IHtmlHelper"/> and <see cref="IHtmlHelper{TModel}"/>.
    /// </summary>
    public static class HtmlHelperDisplayNameExtensions
    {
        /// <summary>
        /// Returns the display name for the current model.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <returns>A <see cref="string"/> containing the display name.</returns>
        public static string DisplayNameForModel(this IHtmlHelper htmlHelper)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.DisplayName(expression: null);
        }

        /// <summary>
        /// Returns the display name for the specified <paramref name="expression"/>
        /// if the current model represents a collection.
        /// </summary>
        /// <param name="htmlHelper">
        /// The <see cref="IHtmlHelper{T}"/> of <see cref="IEnumerable{TModelItem}"/> instance this method extends.
        /// </param>
        /// <param name="expression">An expression to be evaluated against an item in the current model.</param>
        /// <typeparam name="TModelItem">The type of items in the model collection.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A <see cref="string"/> containing the display name.</returns>
        public static string DisplayNameFor<TModelItem, TResult>(
            this IHtmlHelper<IEnumerable<TModelItem>> htmlHelper,
            Expression<Func<TModelItem, TResult>> expression)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return htmlHelper.DisplayNameForInnerType(expression);
        }
    }
}

// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
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
        public static string DisplayNameForModel([NotNull] this IHtmlHelper htmlHelper)
        {
            return htmlHelper.DisplayName(expression: null);
        }

        /// <summary>
        /// Returns the display name for the specified <paramref name="expression"/>
        /// if the current model represents a collection.
        /// </summary>
        /// <param name="htmlHelper">
        /// The <see cref="IHtmlHelper{IEnumerable{TModelItem}}"/> instance this method extends.
        /// </param>
        /// <param name="expression">An expression to be evaluated against an item in the current model.</param>
        /// <typeparam name="TModelItem">The type of items in the model collection.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A <see cref="string"/> containing the display name.</returns>
        public static string DisplayNameFor<TModelItem, TResult>(
            [NotNull] this IHtmlHelper<IEnumerable<TModelItem>> htmlHelper,
            [NotNull] Expression<Func<TModelItem, TResult>> expression)
        {
            return htmlHelper.DisplayNameForInnerType<TModelItem, TResult>(expression);
        }
    }
}

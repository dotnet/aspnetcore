// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Value-related extensions for <see cref="IHtmlHelper"/> and <see cref="IHtmlHelper{TModel}"/>.
    /// </summary>
    public static class HtmlHelperValueExtensions
    {
        /// <summary>
        /// Returns the formatted value for the specified expression <paramref name="name"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="name">Expression name, relative to the current model.</param>
        /// <returns>A <see langref="string"/> containing the formatted value.</returns>
        /// <remarks>
        /// Converts the expression <paramref name="name"/> result to a <see langref="string"/> directly.
        /// </remarks>
        public static string Value([NotNull] this IHtmlHelper htmlHelper, string name)
        {
            return htmlHelper.Value(name, format: null);
        }

        /// <summary>
        /// Returns the formatted value for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">The expression to be evaluated against the current model.</param>
        /// <typeparam name="TModel">The <see cref="Type"/> of the model.</typeparam>
        /// <typeparam name="TProperty">The <see cref="Type"/> of the <param name="expression"> result.</typeparam>
        /// <returns>A <see langref="string"/> containing the formatted value.</returns>
        /// <remarks>
        /// Converts the <paramref name="expression"/> result to a <see langref="string"/> directly.
        /// </remarks>
        public static string ValueFor<TModel, TProperty>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression)
        {
            return htmlHelper.ValueFor(expression, format: null);
        }

        /// <summary>
        /// Returns the formatted value for the current model.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <returns>A <see langref="string"/> containing the formatted value.</returns>
        /// <remarks>
        /// Converts the model value to a <see langref="string"/> directly.
        /// </remarks>
        public static string ValueForModel([NotNull] this IHtmlHelper htmlHelper)
        {
            return htmlHelper.Value(name: string.Empty, format: null);
        }

        /// <summary>
        /// Returns the formatted value for the current model.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="format">
        /// The composite format <see langref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
        /// </param>
        /// <returns>A <see langref="string"/> containing the formatted value.</returns>
        /// <remarks>
        /// Converts the model value to a <see langref="string"/> directly if
        /// <paramref name="format"/> is <see langref="null"/> or empty.
        /// </remarks>
        public static string ValueForModel([NotNull] this IHtmlHelper htmlHelper, string format)
        {
            return htmlHelper.Value(name: string.Empty, format: format);
        }
    }
}

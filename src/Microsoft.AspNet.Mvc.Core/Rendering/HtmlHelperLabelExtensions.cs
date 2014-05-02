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

using System;
using System.Linq.Expressions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class HtmlHelperLabelExtensions
    {
        public static HtmlString Label([NotNull] this IHtmlHelper html, string expression)
        {
            return html.Label(expression,
                             labelText: null,
                             htmlAttributes: null);
        }

        public static HtmlString Label([NotNull] this IHtmlHelper html, string expression, string labelText)
        {
            return html.Label(expression, labelText, htmlAttributes: null);
        }

        public static HtmlString LabelFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html,
                                                          [NotNull] Expression<Func<TModel, TValue>> expression)
        {
            return html.LabelFor<TValue>(expression, labelText: null, htmlAttributes: null);
        }

        public static HtmlString LabelFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html,
                                                          [NotNull] Expression<Func<TModel, TValue>> expression,
                                                          string labelText)
        {
            return html.LabelFor<TValue>(expression, labelText, htmlAttributes: null);
        }

        public static HtmlString LabelFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html,
                                                          [NotNull] Expression<Func<TModel, TValue>> expression,
                                                          object htmlAttributes)
        {
            return html.LabelFor<TValue>(expression, labelText: null, htmlAttributes: htmlAttributes);
        }

        public static HtmlString LabelForModel([NotNull] this IHtmlHelper html)
        {
            return LabelForModel(html, labelText: null);
        }

        public static HtmlString LabelForModel([NotNull] this IHtmlHelper html, string labelText)
        {
            return html.Label(expression: string.Empty, labelText: labelText, htmlAttributes: null);
        }

        public static HtmlString LabelForModel([NotNull] this IHtmlHelper html, object htmlAttributes)
        {
            return html.Label(expression: string.Empty, labelText: null, htmlAttributes: htmlAttributes);
        }

        public static HtmlString LabelForModel(
            [NotNull] this IHtmlHelper html,
            string labelText,
            object htmlAttributes)
        {
            return html.Label(expression: string.Empty, labelText: labelText, htmlAttributes: htmlAttributes);
        }
    }
}
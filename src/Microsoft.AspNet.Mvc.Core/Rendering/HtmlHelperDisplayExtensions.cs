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
    public static class HtmlHelperDisplayExtensions
    {
        public static HtmlString Display([NotNull] this IHtmlHelper html, string expression)
        {
            return html.Display(expression, templateName: null, htmlFieldName: null, additionalViewData: null);
        }

        public static HtmlString Display(
            [NotNull] this IHtmlHelper html,
            string expression,
            object additionalViewData)
        {
            return html.Display(expression, templateName: null, htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        public static HtmlString Display(
            [NotNull] this IHtmlHelper html,
            string expression,
            string templateName)
        {
            return html.Display(expression, templateName, htmlFieldName: null, additionalViewData: null);
        }

        public static HtmlString Display(
            [NotNull] this IHtmlHelper html,
            string expression,
            string templateName,
            object additionalViewData)
        {
            return html.Display(expression, templateName, htmlFieldName: null, additionalViewData: additionalViewData);
        }

        public static HtmlString Display(
            [NotNull] this IHtmlHelper html,
            string expression,
            string templateName,
            string htmlFieldName)
        {
            return html.Display(expression, templateName, htmlFieldName, additionalViewData: null);
        }

        public static HtmlString DisplayFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html,
                                                            [NotNull] Expression<Func<TModel, TValue>> expression)
        {
            return html.DisplayFor<TValue>(expression, templateName: null, htmlFieldName: null,
                additionalViewData: null);
        }

        public static HtmlString DisplayFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html,
                                                            [NotNull] Expression<Func<TModel, TValue>> expression,
                                                            object additionalViewData)
        {
            return html.DisplayFor<TValue>(expression, templateName: null, htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        public static HtmlString DisplayFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html,
                                                            [NotNull] Expression<Func<TModel, TValue>> expression,
                                                            string templateName)
        {
            return html.DisplayFor<TValue>(expression, templateName, htmlFieldName: null, additionalViewData: null);
        }

        public static HtmlString DisplayFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html,
                                                            [NotNull] Expression<Func<TModel, TValue>> expression,
                                                            string templateName,
                                                            object additionalViewData)
        {
            return html.DisplayFor<TValue>(expression, templateName, htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        public static HtmlString DisplayFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html,
                                                            [NotNull] Expression<Func<TModel, TValue>> expression,
                                                            string templateName,
                                                            string htmlFieldName)
        {
            return html.DisplayFor<TValue>(expression, templateName: templateName, htmlFieldName: htmlFieldName,
                additionalViewData: null);
        }

        public static HtmlString DisplayForModel([NotNull] this IHtmlHelper html)
        {
            return html.DisplayForModel(templateName: null, htmlFieldName: null, additionalViewData: null);
        }

        public static HtmlString DisplayForModel([NotNull] this IHtmlHelper html, object additionalViewData)
        {
            return html.DisplayForModel(templateName: null, htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        public static HtmlString DisplayForModel([NotNull] this IHtmlHelper html, string templateName)
        {
            return html.DisplayForModel(templateName, htmlFieldName: null, additionalViewData: null);
        }

        public static HtmlString DisplayForModel(
            [NotNull] this IHtmlHelper html,
            string templateName,
            object additionalViewData)
        {
            return html.DisplayForModel(templateName, htmlFieldName: null, additionalViewData: additionalViewData);
        }

        public static HtmlString DisplayForModel(
            [NotNull] this IHtmlHelper html,
            string templateName,
            string htmlFieldName)
        {
            return html.DisplayForModel(templateName, htmlFieldName, additionalViewData: null);
        }
    }
}

// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            return html.Display(expression: null, templateName: null, htmlFieldName: null, additionalViewData: null);
        }

        public static HtmlString DisplayForModel([NotNull] this IHtmlHelper html, object additionalViewData)
        {
            return html.Display(expression: null, templateName: null, htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        public static HtmlString DisplayForModel([NotNull] this IHtmlHelper html, string templateName)
        {
            return html.Display(expression: null, templateName: templateName, htmlFieldName: null,
                additionalViewData: null);
        }

        public static HtmlString DisplayForModel(
            [NotNull] this IHtmlHelper html,
            string templateName,
            object additionalViewData)
        {
            return html.Display(expression: null, templateName: templateName, htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        public static HtmlString DisplayForModel(
            [NotNull] this IHtmlHelper html,
            string templateName,
            string htmlFieldName)
        {
            return html.Display(expression: null, templateName: templateName, htmlFieldName: htmlFieldName,
                additionalViewData: null);
        }

        public static HtmlString DisplayForModel(
            [NotNull] this IHtmlHelper html,
            string templateName,
            string htmlFieldName,
            object additionalViewData)
        {
            return html.Display(expression: null, templateName: templateName, htmlFieldName: htmlFieldName,
                additionalViewData: additionalViewData);
        }
    }
}

// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class HtmlHelperValidationExtensions
    {
        public static HtmlString ValidationMessage([NotNull] this IHtmlHelper htmlHelper,
            string expression)
        {
            return htmlHelper.ValidationMessage(expression, message: null, htmlAttributes: null, tag: null);
        }

        public static HtmlString ValidationMessage([NotNull] this IHtmlHelper htmlHelper,
            string expression,
            string message)
        {
            return htmlHelper.ValidationMessage(expression, message, htmlAttributes: null, tag: null);
        }

        public static HtmlString ValidationMessage([NotNull] this IHtmlHelper htmlHelper,
            string expression,
            object htmlAttributes)
        {
            return htmlHelper.ValidationMessage(expression, message: null, htmlAttributes: htmlAttributes, tag: null);
        }

        public static HtmlString ValidationMessage([NotNull] this IHtmlHelper htmlHelper,
            string expression,
            string message,
            string tag)
        {
            return htmlHelper.ValidationMessage(expression, message, htmlAttributes: null, tag: tag);
        }

        public static HtmlString ValidationMessage([NotNull] this IHtmlHelper htmlHelper,
            string expression,
            string message,
            object htmlAttributes)
        {
            return htmlHelper.ValidationMessage(expression, message, htmlAttributes, tag: null);
        }

        public static HtmlString ValidationMessage([NotNull] this IHtmlHelper htmlHelper,
            string expression,
            string message,
            object htmlAttributes,
            string tag)
        {
            return htmlHelper.ValidationMessage(expression, message, htmlAttributes, tag);
        }

        public static HtmlString ValidationMessageFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression)
        {
            return htmlHelper.ValidationMessageFor(expression, message: null, htmlAttributes: null, tag: null);
        }

        public static HtmlString ValidationMessageFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression,
            string message)
        {
            return htmlHelper.ValidationMessageFor(expression, message, htmlAttributes: null, tag: null);
        }

        public static HtmlString ValidationMessageFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression,
            string message,
            object htmlAttributes)
        {
            return htmlHelper.ValidationMessageFor(expression, message, htmlAttributes, tag: null);
        }

        public static HtmlString ValidationMessageFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression,
            string message,
            string tag)
        {
            return htmlHelper.ValidationMessageFor(expression, message, htmlAttributes: null, tag: tag);
        }

        public static HtmlString ValidationMessageFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression,
            string message,
            object htmlAttributes,
            string tag)
        {
            return htmlHelper.ValidationMessageFor(expression, message, htmlAttributes, tag);
        }

        public static HtmlString ValidationSummary([NotNull] this IHtmlHelper htmlHelper)
        {
            return htmlHelper.ValidationSummary(excludePropertyErrors: false,
                message: null,
                htmlAttributes: null,
                tag: null);
        }

        public static HtmlString ValidationSummary([NotNull] this IHtmlHelper htmlHelper, bool excludePropertyErrors)
        {
            return htmlHelper.ValidationSummary(excludePropertyErrors,
                message: null,
                htmlAttributes: null,
                tag: null);
        }

        public static HtmlString ValidationSummary([NotNull] this IHtmlHelper htmlHelper, string message)
        {
            return htmlHelper.ValidationSummary(excludePropertyErrors: false,
                message: message,
                htmlAttributes: null,
                tag: null);
        }

        public static HtmlString ValidationSummary([NotNull] this IHtmlHelper htmlHelper, string message, string tag)
        {
            return htmlHelper.ValidationSummary(excludePropertyErrors: false,
                message: message,
                htmlAttributes: null,
                tag: tag);
        }

        public static HtmlString ValidationSummary([NotNull] this IHtmlHelper htmlHelper,
            bool excludePropertyErrors,
            string message)
        {
            return htmlHelper.ValidationSummary(excludePropertyErrors,
                message,
                htmlAttributes: null,
                tag: null);
        }

        public static HtmlString ValidationSummary([NotNull] this IHtmlHelper htmlHelper,
            string message,
            object htmlAttributes)
        {
            return htmlHelper.ValidationSummary(excludePropertyErrors: false,
                message: message,
                htmlAttributes: HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes),
                tag: null);
        }

        public static HtmlString ValidationSummary([NotNull] this IHtmlHelper htmlHelper,
            string message,
            object htmlAttributes,
            string tag)
        {
            return htmlHelper.ValidationSummary(excludePropertyErrors: false,
                message: message,
                htmlAttributes: HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes),
                tag: tag);
        }

        public static HtmlString ValidationSummary([NotNull] this IHtmlHelper htmlHelper,
            bool excludePropertyErrors,
            string message,
            string tag)
        {
            return htmlHelper.ValidationSummary(excludePropertyErrors,
                message,
                htmlAttributes: null,
                tag: tag);
        }

        public static HtmlString ValidationSummary([NotNull] this IHtmlHelper htmlHelper,
            bool excludePropertyErrors,
            string message,
            object htmlAttributes)
        {
            return htmlHelper.ValidationSummary(excludePropertyErrors,
                message,
                HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes),
                tag: null);
        }

        public static HtmlString ValidationSummary([NotNull] this IHtmlHelper htmlHelper,
            bool excludePropertyErrors,
            string message,
            object htmlAttributes,
            string tag)
        {
            return htmlHelper.ValidationSummary(excludePropertyErrors,
                message,
                HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes),
                tag);
        }

        public static HtmlString ValidationSummary([NotNull] this IHtmlHelper htmlHelper,
            string message,
            IDictionary<string, object> htmlAttributes)
        {
            return htmlHelper.ValidationSummary(excludePropertyErrors: false,
                message: message,
                htmlAttributes: htmlAttributes,
                tag: null);
        }

        public static HtmlString ValidationSummary([NotNull] this IHtmlHelper htmlHelper,
            string message,
            IDictionary<string, object> htmlAttributes,
            string tag)
        {
            return htmlHelper.ValidationSummary(excludePropertyErrors: false,
                message: message,
                htmlAttributes: htmlAttributes,
                tag: tag);
        }
    }
}

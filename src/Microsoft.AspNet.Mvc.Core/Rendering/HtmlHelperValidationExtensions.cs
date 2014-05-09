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
            return htmlHelper.ValidationMessage(expression, message: null, htmlAttributes: null);
        }

        public static HtmlString ValidationMessage([NotNull] this IHtmlHelper htmlHelper,
            string expression, string message)
        {
            return htmlHelper.ValidationMessage(expression, message, htmlAttributes: null);
        }

        public static HtmlString ValidationMessage([NotNull] this IHtmlHelper htmlHelper,
            string expression, object htmlAttributes)
        {
            return htmlHelper.ValidationMessage(expression, message: null, htmlAttributes: htmlAttributes);
        }

        public static HtmlString ValidationMessage([NotNull] this IHtmlHelper htmlHelper,
            string expression, string message, object htmlAttributes)
        {
            return htmlHelper.ValidationMessage(expression, message, htmlAttributes);
        }

        public static HtmlString ValidationMessageFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression)
        {
            return htmlHelper.ValidationMessageFor(expression, message: null, htmlAttributes: null);
        }

        public static HtmlString ValidationMessageFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, string message)
        {
            return htmlHelper.ValidationMessageFor(expression, message, htmlAttributes: null);
        }

        public static HtmlString ValidationMessageFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, string message, object htmlAttributes)
        {
            return htmlHelper.ValidationMessageFor(expression, message, htmlAttributes);
        }

        public static HtmlString ValidationSummary([NotNull] this IHtmlHelper htmlHelper)
        {
            return ValidationSummary(htmlHelper, excludePropertyErrors: false);
        }

        public static HtmlString ValidationSummary([NotNull] this IHtmlHelper htmlHelper, bool excludePropertyErrors)
        {
            return ValidationSummary(htmlHelper, excludePropertyErrors, message: null);
        }

        public static HtmlString ValidationSummary([NotNull] this IHtmlHelper htmlHelper, string message)
        {
            return ValidationSummary(htmlHelper, excludePropertyErrors: false, message: message,
                htmlAttributes: (object)null);
        }

        public static HtmlString ValidationSummary(
            [NotNull] this IHtmlHelper htmlHelper,
            bool excludePropertyErrors,
            string message)
        {
            return ValidationSummary(htmlHelper, excludePropertyErrors, message, htmlAttributes: (object)null);
        }

        public static HtmlString ValidationSummary(
            [NotNull] this IHtmlHelper htmlHelper,
            string message,
            object htmlAttributes)
        {
            return ValidationSummary(htmlHelper, excludePropertyErrors: false, message: message,
                htmlAttributes: HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static HtmlString ValidationSummary(
            [NotNull] this IHtmlHelper htmlHelper,
            bool excludePropertyErrors,
            string message,
            object htmlAttributes)
        {
            return htmlHelper.ValidationSummary(excludePropertyErrors, message,
                HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static HtmlString ValidationSummary(
            [NotNull] this IHtmlHelper htmlHelper,
            string message,
            IDictionary<string, object> htmlAttributes)
        {
            return htmlHelper.ValidationSummary(excludePropertyErrors: false, message: message,
                htmlAttributes: htmlAttributes);
        }
    }
}

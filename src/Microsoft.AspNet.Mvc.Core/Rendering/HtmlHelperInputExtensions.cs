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
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class HtmlHelperInputExtensions
    {
        public static HtmlString CheckBox([NotNull] this IHtmlHelper htmlHelper, string name)
        {
            return htmlHelper.CheckBox(name, isChecked: null, htmlAttributes: null);
        }

        public static HtmlString CheckBox(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            bool isChecked)
        {
            return htmlHelper.CheckBox(name, isChecked, htmlAttributes: null);
        }

        public static HtmlString CheckBox(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            object htmlAttributes)
        {
            return htmlHelper.CheckBox(name, isChecked: null, htmlAttributes: htmlAttributes);
        }

        public static HtmlString CheckBoxFor<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, bool>> expression)
        {
            return htmlHelper.CheckBoxFor(expression, htmlAttributes: null);
        }

        public static HtmlString Hidden([NotNull] this IHtmlHelper htmlHelper, string name)
        {
            return htmlHelper.Hidden(name, value: null, htmlAttributes: null);
        }

        public static HtmlString Hidden(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            object value)
        {
            return htmlHelper.Hidden(name, value, htmlAttributes: null);
        }

        public static HtmlString HiddenFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression)
        {
            return htmlHelper.HiddenFor(expression, htmlAttributes: null);
        }

        public static HtmlString Password([NotNull] this IHtmlHelper htmlHelper, string name)
        {
            return htmlHelper.Password(name, value: null, htmlAttributes: null);
        }

        public static HtmlString Password(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            object value)
        {
            return htmlHelper.Password(name, value, htmlAttributes: null);
        }

        public static HtmlString PasswordFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression)
        {
            return htmlHelper.PasswordFor(expression, htmlAttributes: null);
        }

        public static HtmlString RadioButton(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            object value)
        {
            return htmlHelper.RadioButton(name, value, isChecked: null, htmlAttributes: null);
        }

        public static HtmlString RadioButton(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            object value,
            object htmlAttributes)
        {
            return htmlHelper.RadioButton(name, value, isChecked: null, htmlAttributes: htmlAttributes);
        }

        public static HtmlString RadioButton(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            object value,
            bool isChecked)
        {
            return htmlHelper.RadioButton(name, value, isChecked, htmlAttributes: null);
        }

        public static HtmlString RadioButtonFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, object value)
        {
            return htmlHelper.RadioButtonFor(expression, value, htmlAttributes: null);
        }

        public static HtmlString TextBox([NotNull] this IHtmlHelper htmlHelper, string name)
        {
            return TextBox(htmlHelper, name, value: null);
        }

        public static HtmlString TextBox(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            object value)
        {
            return TextBox(htmlHelper, name, value, format: null);
        }

        public static HtmlString TextBox(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            object value,
            string format)
        {
            return TextBox(htmlHelper, name, value, format, htmlAttributes: null);
        }

        public static HtmlString TextBox(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            object value,
            object htmlAttributes)
        {
            return TextBox(htmlHelper, name, value, format: null, htmlAttributes: htmlAttributes);
        }

        public static HtmlString TextBox(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            object value,
            string format,
            object htmlAttributes)
        {
            return htmlHelper.TextBox(name, value, format,
                HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static HtmlString TextBox(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            object value,
            IDictionary<string, object> htmlAttributes)
        {
            return htmlHelper.TextBox(name, value, format: null, htmlAttributes: htmlAttributes);
        }

        public static HtmlString TextBoxFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression)
        {
            return TextBoxFor(htmlHelper, expression, format: null);
        }

        public static HtmlString TextBoxFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, string format)
        {
            return TextBoxFor(htmlHelper, expression, format, htmlAttributes: null);
        }

        public static HtmlString TextBoxFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, object htmlAttributes)
        {
            return TextBoxFor(htmlHelper, expression, format: null, htmlAttributes: htmlAttributes);
        }

        public static HtmlString TextBoxFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, string format, object htmlAttributes)
        {
            return htmlHelper.TextBoxFor(expression, format: format,
                htmlAttributes: HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static HtmlString TextBoxFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, IDictionary<string, object> htmlAttributes)
        {
            return htmlHelper.TextBoxFor(expression, format: null, htmlAttributes: htmlAttributes);
        }

        public static HtmlString TextArea([NotNull] this IHtmlHelper htmlHelper,
            string name)
        {
            return htmlHelper.TextArea(name, value: null, rows: 0, columns: 0, htmlAttributes: null);
        }

        public static HtmlString TextArea([NotNull] this IHtmlHelper htmlHelper,
            string name, object htmlAttributes)
        {
            return htmlHelper.TextArea(name, value: null, rows: 0, columns:0, htmlAttributes: htmlAttributes);
        }

        public static HtmlString TextArea([NotNull] this IHtmlHelper htmlHelper,
            string name, string value)
        {
            return htmlHelper.TextArea(name, value, rows: 0, columns: 0, htmlAttributes: null);
        }

        public static HtmlString TextArea([NotNull] this IHtmlHelper htmlHelper,
            string name, string value, object htmlAttributes)
        {
            return htmlHelper.TextArea(name, value, rows: 0, columns: 0, htmlAttributes: htmlAttributes);
        }

        public static HtmlString TextArea([NotNull] this IHtmlHelper htmlHelper,
            string name, string value, int rows, int columns, object htmlAttributes)
        {
            return htmlHelper.TextArea(name, value, rows, columns, htmlAttributes);
        }

        public static HtmlString TextAreaFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression)
        {
            return htmlHelper.TextAreaFor(expression, rows: 0, columns: 0, htmlAttributes: null);
        }

        public static HtmlString TextAreaFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, object htmlAttributes)
        {
            return htmlHelper.TextAreaFor(expression, rows: 0, columns: 0, htmlAttributes: htmlAttributes);
        }

        public static HtmlString TextAreaFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, int rows, int columns, object htmlAttributes)
        {
            return htmlHelper.TextAreaFor(expression, rows, columns, htmlAttributes);
        }
    }
}

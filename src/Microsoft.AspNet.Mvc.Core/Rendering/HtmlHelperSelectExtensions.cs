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
    public static class SelectExtensions
    {
        public static HtmlString DropDownList([NotNull] this IHtmlHelper htmlHelper, string name)
        {
            return htmlHelper.DropDownList(name, selectList: null, optionLabel: null, htmlAttributes: null);
        }

        public static HtmlString DropDownList([NotNull] this IHtmlHelper htmlHelper, string name, string optionLabel)
        {
            return htmlHelper.DropDownList(name, selectList: null, optionLabel: optionLabel, htmlAttributes: null);
        }

        public static HtmlString DropDownList(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            IEnumerable<SelectListItem> selectList)
        {
            return htmlHelper.DropDownList(name, selectList, optionLabel: null, htmlAttributes: null);
        }

        public static HtmlString DropDownList(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            IEnumerable<SelectListItem> selectList,
            object htmlAttributes)
        {
            return htmlHelper.DropDownList(name, selectList, optionLabel: null, htmlAttributes: htmlAttributes);
        }

        public static HtmlString DropDownList(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            IEnumerable<SelectListItem> selectList,
            string optionLabel)
        {
            return htmlHelper.DropDownList(name, selectList, optionLabel, htmlAttributes: null);
        }

        public static HtmlString DropDownListFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList)
        {
            return htmlHelper.DropDownListFor(expression, selectList, optionLabel: null, htmlAttributes: null);
        }

        public static HtmlString DropDownListFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList,
            object htmlAttributes)
        {
            return htmlHelper.DropDownListFor(expression, selectList, optionLabel: null,
                htmlAttributes: htmlAttributes);
        }

        public static HtmlString DropDownListFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList,
            string optionLabel)
        {
            return htmlHelper.DropDownListFor(expression, selectList, optionLabel, htmlAttributes: null);
        }

        public static HtmlString ListBox([NotNull] this IHtmlHelper htmlHelper, string name)
        {
            return htmlHelper.ListBox(name, selectList: null, htmlAttributes: null);
        }

        public static HtmlString ListBox(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            IEnumerable<SelectListItem> selectList)
        {
            return htmlHelper.ListBox(name, selectList, htmlAttributes: null);
        }

        public static HtmlString ListBoxFor<TModel, TProperty>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression,
            IEnumerable<SelectListItem> selectList)
        {
            return htmlHelper.ListBoxFor(expression, selectList, htmlAttributes: null);
        }
    }
}

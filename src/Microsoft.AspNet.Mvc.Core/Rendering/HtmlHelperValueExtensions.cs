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
    public static class HtmlHelperValueExtensions
    {
        public static HtmlString Value([NotNull] this IHtmlHelper htmlHelper, string name)
        {
            return htmlHelper.Value(name, format: null);
        }

        public static HtmlString ValueFor<TModel, TProperty>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression)
        {
            return htmlHelper.ValueFor(expression, format: null);
        }

        public static HtmlString ValueForModel([NotNull] this IHtmlHelper htmlHelper)
        {
            return ValueForModel(htmlHelper, format: null);
        }

        public static HtmlString ValueForModel([NotNull] this IHtmlHelper htmlHelper, string format)
        {
            return htmlHelper.Value(string.Empty, format);
        }
    }
}

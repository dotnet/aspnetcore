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

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class HtmlHelperFormExtensions
    {
        public static MvcForm BeginForm([NotNull] this IHtmlHelper htmlHelper)
        {
            // Generates <form action="{current url}" method="post">.
            return htmlHelper.BeginForm(actionName: null, controllerName: null, routeValues: null,
                                        method: FormMethod.Post, htmlAttributes: null);
        }

        public static MvcForm BeginForm([NotNull] this IHtmlHelper htmlHelper, FormMethod method)
        {
            return htmlHelper.BeginForm(actionName: null, controllerName: null, routeValues: null,
                                        method: method, htmlAttributes: null);
        }

        public static MvcForm BeginForm(
            [NotNull] this IHtmlHelper htmlHelper,
            FormMethod method,
            object htmlAttributes)
        {
            return htmlHelper.BeginForm(actionName: null, controllerName: null, routeValues: null,
                                        method: method, htmlAttributes: htmlAttributes);
        }

        public static MvcForm BeginForm([NotNull] this IHtmlHelper htmlHelper, object routeValues)
        {
            return htmlHelper.BeginForm(actionName: null, controllerName: null, routeValues: routeValues,
                                        method: FormMethod.Post, htmlAttributes: null);
        }

        public static MvcForm BeginForm(
            [NotNull] this IHtmlHelper htmlHelper,
            string actionName,
            string controllerName)
        {
            return htmlHelper.BeginForm(actionName, controllerName, routeValues: null,
                                        method: FormMethod.Post, htmlAttributes: null);
        }

        public static MvcForm BeginForm(
            [NotNull] this IHtmlHelper htmlHelper,
            string actionName,
            string controllerName,
            object routeValues)
        {
            return htmlHelper.BeginForm(actionName, controllerName, routeValues,
                                        FormMethod.Post, htmlAttributes: null);
        }

        public static MvcForm BeginForm(
            [NotNull] this IHtmlHelper htmlHelper,
            string actionName,
            string controllerName,
            FormMethod method)
        {
            return htmlHelper.BeginForm(actionName, controllerName, routeValues: null,
                                        method: method, htmlAttributes: null);
        }

        public static MvcForm BeginForm(
            [NotNull] this IHtmlHelper htmlHelper,
            string actionName,
            string controllerName,
            object routeValues,
            FormMethod method)
        {
            return htmlHelper.BeginForm(actionName, controllerName, routeValues,
                                        method, htmlAttributes: null);
        }

        public static MvcForm BeginForm(
            [NotNull] this IHtmlHelper htmlHelper,
            string actionName,
            string controllerName,
            FormMethod method,
            object htmlAttributes)
        {
            return htmlHelper.BeginForm(actionName, controllerName, routeValues: null,
                                        method: method, htmlAttributes: htmlAttributes);
        }
    }
}

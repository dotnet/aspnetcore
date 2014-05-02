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
    public static class HtmlHelperLinkExtensions
    {
        public static HtmlString ActionLink(
            [NotNull] this IHtmlHelper helper,
            [NotNull] string linkText,
            string actionName)
        {
            return helper.ActionLink(
                linkText,
                actionName,
                controllerName: null,
                protocol: null,
                hostname: null,
                fragment: null,
                routeValues: null,
                htmlAttributes: null);
        }

        public static HtmlString ActionLink(
            [NotNull] this IHtmlHelper helper,
            [NotNull] string linkText,
            string actionName,
            object routeValues)
        {
            return helper.ActionLink(
                linkText,
                actionName,
                controllerName: null,
                protocol: null,
                hostname: null,
                fragment: null,
                routeValues: routeValues,
                htmlAttributes: null);
        }

        public static HtmlString ActionLink(
            [NotNull] this IHtmlHelper helper,
            [NotNull] string linkText,
            string actionName,
            object routeValues,
            object htmlAttributes)
        {
            return helper.ActionLink(
                linkText,
                actionName,
                controllerName: null,
                protocol: null,
                hostname: null,
                fragment: null,
                routeValues: routeValues,
                htmlAttributes: htmlAttributes);
        }

        public static HtmlString ActionLink(
            [NotNull] this IHtmlHelper helper,
            [NotNull] string linkText,
            string actionName,
            string controllerName)
        {
            return helper.ActionLink(
                linkText,
                actionName,
                controllerName,
                protocol: null,
                hostname: null,
                fragment: null,
                routeValues: null,
                htmlAttributes: null);
        }

        public static HtmlString ActionLink(
            [NotNull] this IHtmlHelper helper,
            [NotNull] string linkText,
            string actionName,
            string controllerName,
            object routeValues)
        {
            return helper.ActionLink(
                linkText,
                actionName,
                controllerName,
                protocol: null,
                hostname: null,
                fragment: null,
                routeValues: routeValues,
                htmlAttributes: null);
        }

        public static HtmlString ActionLink(
            [NotNull] this IHtmlHelper helper,
            [NotNull] string linkText,
            string actionName,
            string controllerName,
            object routeValues,
            object htmlAttributes)
        {
            return helper.ActionLink(
                linkText,
                actionName,
                controllerName,
                protocol: null,
                hostname: null,
                fragment: null,
                routeValues: routeValues,
                htmlAttributes: htmlAttributes);
        }

        public static HtmlString RouteLink(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string linkText,
            object routeValues)
        {
            return htmlHelper.RouteLink(
                                linkText,
                                routeName: null,
                                protocol: null,
                                hostName: null,
                                fragment: null,
                                routeValues: routeValues,
                                htmlAttributes: null);
        }

        public static HtmlString RouteLink(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string linkText,
            string routeName)
        {
            return htmlHelper.RouteLink(
                                linkText,
                                routeName,
                                protocol: null,
                                hostName: null,
                                fragment: null,
                                routeValues: null,
                                htmlAttributes: null);
        }

        public static HtmlString RouteLink(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string linkText,
            string routeName,
            object routeValues)
        {
            return htmlHelper.RouteLink(
                                linkText,
                                routeName,
                                protocol: null,
                                hostName: null,
                                fragment: null,
                                routeValues: routeValues,
                                htmlAttributes: null);
        }

        public static HtmlString RouteLink(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string linkText,
            object routeValues,
            object htmlAttributes)
        {
            return htmlHelper.RouteLink(
                                linkText,
                                routeName: null,
                                protocol: null,
                                hostName: null,
                                fragment: null,
                                routeValues: routeValues,
                                htmlAttributes: htmlAttributes);
        }

        public static HtmlString RouteLink(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string linkText,
            string routeName,
            object routeValues,
            object htmlAttributes)
        {
            return htmlHelper.RouteLink(
                                 linkText,
                                 routeName,
                                 protocol: null,
                                 hostName: null,
                                 fragment: null,
                                 routeValues: routeValues,
                                 htmlAttributes: htmlAttributes);
        }

        public static HtmlString RouteLink(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string linkText,
            string routeName,
            string protocol,
            string hostName,
            string fragment,
            object routeValues,
            object htmlAttributes)
        {
            return htmlHelper.RouteLink(
                                 linkText,
                                 routeName,
                                 protocol,
                                 hostName,
                                 fragment,
                                 routeValues,
                                 htmlAttributes);
        }
    }
}

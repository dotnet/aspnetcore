// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public static class DefaultHtmlGeneratorExtensions
    {
        public static TagBuilder GenerateForm(
            this IHtmlGenerator generator,
            ViewContext viewContext,
            string actionName,
            string controllerName,
            string fragment,
            object routeValues,
            string method,
            object htmlAttributes)
        {
            var tagBuilder = generator.GenerateForm(viewContext, actionName, controllerName, routeValues, method, htmlAttributes);

            // Append the fragment to action
            if (fragment != null)
            {
                tagBuilder.Attributes["action"] += "#" + fragment;
            }

            return tagBuilder;
        }

        public static TagBuilder GenerateRouteForm(
            this IHtmlGenerator generator,
            ViewContext viewContext,
            string routeName,
            object routeValues,
            string fragment,
            string method,
            object htmlAttributes)
        {
            var tagBuilder = generator.GenerateRouteForm(viewContext, routeName, routeValues, method, htmlAttributes);

            // Append the fragment to action
            if (fragment != null)
            {
                tagBuilder.Attributes["action"] += "#" + fragment;
            }

            return tagBuilder;
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SpaServices;

// Putting in this namespace so it's always available whenever MapRoute is

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods useful for configuring routing in a single-page application (SPA).
    /// </summary>
    [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public static class SpaRouteExtensions
    {
        private const string ClientRouteTokenName = "clientRoute";

        /// <summary>
        /// Configures a route that is automatically bypassed if the requested URL appears to be for a static file
        /// (e.g., if it has a filename extension).
        /// </summary>
        /// <param name="routeBuilder">The <see cref="IRouteBuilder"/>.</param>
        /// <param name="name">The route name.</param>
        /// <param name="defaults">Default route parameters.</param>
        /// <param name="constraints">Route constraints.</param>
        /// <param name="dataTokens">Route data tokens.</param>
        public static void MapSpaFallbackRoute(
            this IRouteBuilder routeBuilder,
            string name,
            object defaults,
            object constraints = null,
            object dataTokens = null)
        {
            MapSpaFallbackRoute(
                routeBuilder,
                name,
                /* templatePrefix */ null,
                defaults,
                constraints,
                dataTokens);
        }

        /// <summary>
        /// Configures a route that is automatically bypassed if the requested URL appears to be for a static file
        /// (e.g., if it has a filename extension).
        /// </summary>
        /// <param name="routeBuilder">The <see cref="IRouteBuilder"/>.</param>
        /// <param name="name">The route name.</param>
        /// <param name="templatePrefix">The template prefix.</param>
        /// <param name="defaults">Default route parameters.</param>
        /// <param name="constraints">Route constraints.</param>
        /// <param name="dataTokens">Route data tokens.</param>
        public static void MapSpaFallbackRoute(
            this IRouteBuilder routeBuilder,
            string name,
            string templatePrefix,
            object defaults,
            object constraints = null,
            object dataTokens = null)
        {
            var template = CreateRouteTemplate(templatePrefix);
            var constraintsDict = ObjectToDictionary(constraints);
            constraintsDict.Add(ClientRouteTokenName, new SpaRouteConstraint(ClientRouteTokenName));

            routeBuilder.MapRoute(name, template, defaults, constraintsDict, dataTokens);
        }

        private static string CreateRouteTemplate(string templatePrefix)
        {
            templatePrefix = templatePrefix ?? string.Empty;

            if (templatePrefix.Contains("?"))
            {
                // TODO: Consider supporting this. The {*clientRoute} part should be added immediately before the '?'
                throw new ArgumentException("SPA fallback route templates don't support querystrings");
            }

            if (templatePrefix.Contains("#"))
            {
                throw new ArgumentException(
                    "SPA fallback route templates should not include # characters. The hash part of a URI does not get sent to the server.");
            }

            if (templatePrefix != string.Empty && !templatePrefix.EndsWith("/"))
            {
                templatePrefix += "/";
            }

            return templatePrefix + $"{{*{ClientRouteTokenName}}}";
        }

        private static IDictionary<string, object> ObjectToDictionary(object value)
            => value as IDictionary<string, object> ?? new RouteValueDictionary(value);
    }
}

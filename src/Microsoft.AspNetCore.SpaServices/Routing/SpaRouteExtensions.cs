using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SpaServices;

// Putting in this namespace so it's always available whenever MapRoute is
namespace Microsoft.AspNetCore.Builder
{
    public static class SpaRouteExtensions
    {
        private const string ClientRouteTokenName = "clientRoute";

        public static void MapSpaFallbackRoute(this IRouteBuilder routeBuilder, string name, object defaults, object constraints = null, object dataTokens = null)
        {
            MapSpaFallbackRoute(routeBuilder, name, /* templatePrefix */ (string)null, defaults, constraints, dataTokens);
        }

        public static void MapSpaFallbackRoute(this IRouteBuilder routeBuilder, string name, string templatePrefix, object defaults, object constraints = null, object dataTokens = null)
        {
            var template = CreateRouteTemplate(templatePrefix);

            var constraintsDict = ObjectToDictionary(constraints);
            constraintsDict.Add(ClientRouteTokenName, new SpaRouteConstraint(ClientRouteTokenName));

            routeBuilder.MapRoute(name, template, defaults, constraintsDict, dataTokens);
        }

        private static string CreateRouteTemplate(string templatePrefix)
        {
            templatePrefix = templatePrefix ?? string.Empty;

            if (templatePrefix.Contains("?")) {
                // TODO: Consider supporting this. The {*clientRoute} part should be added immediately before the '?'
                throw new ArgumentException("SPA fallback route templates don't support querystrings");
            }

            if (templatePrefix.Contains("#")) {
                throw new ArgumentException("SPA fallback route templates should not include # characters. The hash part of a URI does not get sent to the server.");
            }

            if (templatePrefix != string.Empty && !templatePrefix.EndsWith("/")) {
                templatePrefix += "/";
            }

            return templatePrefix + $"{{*{ ClientRouteTokenName }}}";
        }

        private static IDictionary<string, object> ObjectToDictionary(object value)
        {
            return value as IDictionary<string, object> ?? new RouteValueDictionary(value);
        }
    }
}

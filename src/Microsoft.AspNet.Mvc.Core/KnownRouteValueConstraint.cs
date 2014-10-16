// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class KnownRouteValueConstraint : IRouteConstraint
    {
        private RouteValuesCollection _cachedValuesCollection;

        public bool Match([NotNull] HttpContext httpContext,
                          [NotNull] IRouter route,
                          [NotNull] string routeKey,
                          [NotNull] IDictionary<string, object> values,
                          RouteDirection routeDirection)
        {
            object value;
            if (values.TryGetValue(routeKey, out value))
            {
                var valueAsString = value as string;

                if (valueAsString != null)
                {
                    var allValues = GetAndCacheAllMatchingValues(routeKey, httpContext);
                    var match = allValues.Any(existingRouteValue => 
                                                existingRouteValue.Equals(
                                                                    valueAsString,
                                                                    StringComparison.OrdinalIgnoreCase));

                    return match;
                }
            }

            return false;
        }

        private string[] GetAndCacheAllMatchingValues(string routeKey, HttpContext httpContext)
        {
            var actionDescriptors = GetAndValidateActionDescriptorsCollection(httpContext);
            var version = actionDescriptors.Version;
            var valuesCollection = _cachedValuesCollection;

            if (valuesCollection == null ||
                version != valuesCollection.Version)
            {
                var routeValueCollection = actionDescriptors
                                            .Items
                                            .Select(ad => ad.RouteConstraints
                                                            .FirstOrDefault(
                                                                c => c.RouteKey == routeKey &&
                                                                c.KeyHandling == RouteKeyHandling.RequireKey))
                                            .Where(rc => rc != null)
                                            .Select(rc => rc.RouteValue)
                                            .Distinct()
                                            .ToArray();

                valuesCollection = new RouteValuesCollection(version, routeValueCollection);
                _cachedValuesCollection = valuesCollection;
            }

            return _cachedValuesCollection.Items;
        }

        private static ActionDescriptorsCollection GetAndValidateActionDescriptorsCollection(HttpContext httpContext)
        {
            var provider = httpContext.RequestServices
                                      .GetRequiredService<IActionDescriptorsCollectionProvider>();
            var descriptors = provider.ActionDescriptors;

            if (descriptors == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatPropertyOfTypeCannotBeNull("ActionDescriptors",
                                                               provider.GetType()));
            }

            return descriptors;
        }

        private class RouteValuesCollection
        {
            public RouteValuesCollection(int version, string[] items)
            {
                Version = version;
                Items = items;
            }

            public int Version { get; private set; }

            public string[] Items { get; private set; }
        }
    }
}

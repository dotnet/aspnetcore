// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    public class KnownRouteValueConstraint : IRouteConstraint
    {
        private RouteValuesCollection _cachedValuesCollection;

        public bool Match(
            HttpContext httpContext,
            IRouter route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (routeKey == null)
            {
                throw new ArgumentNullException(nameof(routeKey));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            object obj;
            if (values.TryGetValue(routeKey, out obj))
            {
                var value = obj as string;
                if (value != null)
                {
                    var allValues = GetAndCacheAllMatchingValues(routeKey, httpContext);
                    foreach (var existingValue in allValues)
                    {
                        if (string.Equals(value, existingValue, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private string[] GetAndCacheAllMatchingValues(string routeKey, HttpContext httpContext)
        {
            var actionDescriptors = GetAndValidateActionDescriptorCollection(httpContext);
            var version = actionDescriptors.Version;
            var valuesCollection = _cachedValuesCollection;

            if (valuesCollection == null ||
                version != valuesCollection.Version)
            {
                var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < actionDescriptors.Items.Count; i++)
                {
                    var action = actionDescriptors.Items[i];

                    string value;
                    if (action.RouteValues.TryGetValue(routeKey, out value) &&
                        !string.IsNullOrEmpty(value))
                    {
                        values.Add(value);
                    }
                }

                valuesCollection = new RouteValuesCollection(version, values.ToArray());
                _cachedValuesCollection = valuesCollection;
            }

            return _cachedValuesCollection.Items;
        }

        private static ActionDescriptorCollection GetAndValidateActionDescriptorCollection(HttpContext httpContext)
        {
            var services = httpContext.RequestServices;
            var provider = services.GetRequiredService<IActionDescriptorCollectionProvider>();
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

            public int Version { get; }

            public string[] Items { get; }
        }
    }
}

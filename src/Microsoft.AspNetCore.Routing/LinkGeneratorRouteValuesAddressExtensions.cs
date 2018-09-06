// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using System;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Extension methods for using <see cref="LinkGenerator"/> with <see cref="RouteValuesAddress"/>.
    /// </summary>
    public static class LinkGeneratorRouteValuesAddressExtensions
    {
        public static string GetPathByRouteValues(
            this LinkGenerator generator,
            HttpContext httpContext,
            string routeName,
            object values,
            FragmentString fragment = default,
            LinkOptions options = default)
        {
            if (generator == null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            var address = CreateAddress(httpContext, routeName, values);
            return generator.GetPathByAddress<RouteValuesAddress>(httpContext, address, address.ExplicitValues, fragment, options);
        }

        public static string GetPathByRouteValues(
            this LinkGenerator generator,
            string routeName,
            object values,
            PathString pathBase = default,
            FragmentString fragment = default,
            LinkOptions options = default)
        {
            if (generator == null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            var address = CreateAddress(httpContext: null, routeName, values);
            return generator.GetPathByAddress<RouteValuesAddress>(address, address.ExplicitValues, pathBase, fragment, options);
        }

        public static string GetUriByRouteValues(
            this LinkGenerator generator,
            HttpContext httpContext,
            string routeName,
            object values,
            FragmentString fragment = default,
            LinkOptions options = default)
        {
            if (generator == null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            var address = CreateAddress(httpContext: null, routeName, values);
            return generator.GetUriByAddress<RouteValuesAddress>(httpContext, address, address.ExplicitValues, fragment, options);
        }

        public static string GetUriByRouteValues(
            this LinkGenerator generator,
            string routeName,
            object values,
            string scheme,
            HostString host,
            PathString pathBase = default,
            FragmentString fragment = default,
            LinkOptions options = default)
        {
            if (generator == null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            var address = CreateAddress(httpContext: null, routeName, values);
            return generator.GetUriByAddress<RouteValuesAddress>(address, address.ExplicitValues, scheme, host, pathBase, fragment, options);
        }


        private static RouteValuesAddress CreateAddress(HttpContext httpContext, string routeName, object values)
        {
            return new RouteValuesAddress()
            {
                AmbientValues = DefaultLinkGenerator.GetAmbientValues(httpContext),
                ExplicitValues = new RouteValueDictionary(values),
                RouteName = routeName,
            };
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;

namespace Microsoft.AspNetCore.Builder
{
    public static class WebApiCompatShimRouteBuilderExtensions
    {
        public static IRouteBuilder MapWebApiRoute(
            this IRouteBuilder routeCollectionBuilder,
            string name,
            string template)
        {
            return MapWebApiRoute(routeCollectionBuilder, name, template, defaults: null);
        }

        public static IRouteBuilder MapWebApiRoute(
            this IRouteBuilder routeCollectionBuilder,
            string name,
            string template,
            object defaults)
        {
            return MapWebApiRoute(routeCollectionBuilder, name, template, defaults, constraints: null);
        }

        public static IRouteBuilder MapWebApiRoute(
            this IRouteBuilder routeCollectionBuilder,
            string name,
            string template,
            object defaults,
            object constraints)
        {
            return MapWebApiRoute(routeCollectionBuilder, name, template, defaults, constraints, dataTokens: null);
        }

        public static IRouteBuilder MapWebApiRoute(
            this IRouteBuilder routeCollectionBuilder,
            string name,
            string template,
            object defaults,
            object constraints,
            object dataTokens)
        {
            var mutableDefaults = ObjectToDictionary(defaults);
            mutableDefaults.Add("area", WebApiCompatShimOptionsSetup.DefaultAreaName);

            var mutableConstraints = ObjectToDictionary(constraints);
            mutableConstraints.Add("area", new RequiredRouteConstraint());

            return routeCollectionBuilder.MapRoute(name, template, mutableDefaults, mutableConstraints, dataTokens);
        }

        private static IDictionary<string, object> ObjectToDictionary(object value)
        {
            var dictionary = value as IDictionary<string, object>;
            if (dictionary != null)
            {
                return new RouteValueDictionary(dictionary);
            }
            else
            {
                return new RouteValueDictionary(value);
            }
        }
    }
}
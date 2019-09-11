// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal static class MatcherAssert
    {
        public static void AssertMatch(EndpointSelectorContext context, HttpContext httpContext, Endpoint expected)
        {
            AssertMatch(context, httpContext, expected, new RouteValueDictionary());
        }

        public static void AssertMatch(EndpointSelectorContext context, HttpContext httpContext, Endpoint expected, bool ignoreValues)
        {
            AssertMatch(context, httpContext, expected, new RouteValueDictionary(), ignoreValues);
        }

        public static void AssertMatch(EndpointSelectorContext context, HttpContext httpContext, Endpoint expected, object values)
        {
            AssertMatch(context, httpContext, expected, new RouteValueDictionary(values));
        }

        public static void AssertMatch(EndpointSelectorContext context, HttpContext httpContext, Endpoint expected, string[] keys, string[] values)
        {
            keys = keys ?? Array.Empty<string>();
            values = values ?? Array.Empty<string>();

            if (keys.Length != values.Length)
            {
                throw new XunitException($"Keys and Values must be the same length.");
            }

            var zipped = keys.Zip(values, (k, v) => new KeyValuePair<string, object>(k, v));
            AssertMatch(context, httpContext, expected, new RouteValueDictionary(zipped));
        }

        public static void AssertMatch(
            EndpointSelectorContext context,
            HttpContext httpContext,
            Endpoint expected,
            RouteValueDictionary values,
            bool ignoreValues = false)
        {
            if (context.Endpoint == null)
            {
                throw new XunitException($"Was expected to match '{expected.DisplayName}' but did not match.");
            }

            var actualValues = httpContext.Features.Get<IRouteValuesFeature>().RouteValues;

            if (actualValues == null)
            {
                throw new XunitException("RouteValues is null.");
            }

            if (!object.ReferenceEquals(expected, context.Endpoint))
            {
                throw new XunitException(
                    $"Was expected to match '{expected.DisplayName}' but matched " +
                    $"'{context.Endpoint.DisplayName}' with values: {FormatRouteValues(actualValues)}.");
            }

            if (!ignoreValues)
            {
                // Note: this comparison is intended for unit testing, and is stricter than necessary to make tests
                // more precise. Route value comparisons in product code are more flexible than a simple .Equals.
                if (values.Count != actualValues.Count ||
                    !values.OrderBy(kvp => kvp.Key).SequenceEqual(actualValues.OrderBy(kvp => kvp.Key)))
                {
                    throw new XunitException(
                        $"Was expected to match '{expected.DisplayName}' with values {FormatRouteValues(values)} but matched " +
                        $"values: {FormatRouteValues(actualValues)}.");
                }
            }
        }

        public static void AssertNotMatch(EndpointSelectorContext context, HttpContext httpContext)
        {
            if (context.Endpoint != null)
            {
                throw new XunitException(
                    $"Was expected not to match '{context.Endpoint.DisplayName}' " +
                    $"but matched with values: {FormatRouteValues(httpContext.Features.Get<IRouteValuesFeature>().RouteValues)}.");
            }
        }

        private static string FormatRouteValues(RouteValueDictionary values)
        {
            return "{" + string.Join(", ", values.Select(kvp => $"{kvp.Key} = '{kvp.Value}'")) + "}";
        }
    }
}
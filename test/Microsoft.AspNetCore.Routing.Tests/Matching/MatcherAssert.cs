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
        public static void AssertMatch(EndpointFeature feature, HttpContext context, Endpoint expected)
        {
            AssertMatch(feature, context, expected, new RouteValueDictionary());
        }

        public static void AssertMatch(EndpointFeature feature, HttpContext context, Endpoint expected, bool ignoreValues)
        {
            AssertMatch(feature, context, expected, new RouteValueDictionary(), ignoreValues);
        }

        public static void AssertMatch(EndpointFeature feature, HttpContext context, Endpoint expected, object values)
        {
            AssertMatch(feature, context, expected, new RouteValueDictionary(values));
        }

        public static void AssertMatch(EndpointFeature feature, HttpContext context, Endpoint expected, string[] keys, string[] values)
        {
            keys = keys ?? Array.Empty<string>();
            values = values ?? Array.Empty<string>();

            if (keys.Length != values.Length)
            {
                throw new XunitException($"Keys and Values must be the same length.");
            }

            var zipped = keys.Zip(values, (k, v) => new KeyValuePair<string, object>(k, v));
            AssertMatch(feature, context, expected, new RouteValueDictionary(zipped));
        }

        public static void AssertMatch(
            EndpointFeature feature,
            HttpContext context,
            Endpoint expected,
            RouteValueDictionary values,
            bool ignoreValues = false)
        {
            if (feature.Endpoint == null)
            {
                throw new XunitException($"Was expected to match '{expected.DisplayName}' but did not match.");
            }

            var actualValues = context.Features.Get<IRouteValuesFeature>().RouteValues;

            if (actualValues == null)
            {
                throw new XunitException("RouteValues is null.");
            }

            if (!object.ReferenceEquals(expected, feature.Endpoint))
            {
                throw new XunitException(
                    $"Was expected to match '{expected.DisplayName}' but matched " +
                    $"'{feature.Endpoint.DisplayName}' with values: {FormatRouteValues(actualValues)}.");
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

        public static void AssertNotMatch(EndpointFeature feature, HttpContext context)
        {
            if (feature.Endpoint != null)
            {
                throw new XunitException(
                    $"Was expected not to match '{feature.Endpoint.DisplayName}' " +
                    $"but matched with values: {FormatRouteValues(context.Features.Get<IRouteValuesFeature>().RouteValues)}.");
            }
        }

        private static string FormatRouteValues(RouteValueDictionary values)
        {
            return "{" + string.Join(", ", values.Select(kvp => $"{kvp.Key} = '{kvp.Value}'")) + "}";
        }
    }
}
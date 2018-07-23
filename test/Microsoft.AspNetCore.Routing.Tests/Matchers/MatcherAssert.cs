// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal static class MatcherAssert
    {
        public static void AssertMatch(IEndpointFeature feature, Endpoint expected)
        {
            AssertMatch(feature, expected, new RouteValueDictionary());
        }

        public static void AssertMatch(IEndpointFeature feature, Endpoint expected, bool ignoreValues)
        {
            AssertMatch(feature, expected, new RouteValueDictionary(), ignoreValues);
        }

        public static void AssertMatch(IEndpointFeature feature, Endpoint expected, object values)
        {
            AssertMatch(feature, expected, new RouteValueDictionary(values));
        }

        public static void AssertMatch(IEndpointFeature feature, Endpoint expected, string[] keys, string[] values)
        {
            keys = keys ?? Array.Empty<string>();
            values = values ?? Array.Empty<string>();

            if (keys.Length != values.Length)
            {
                throw new XunitException($"Keys and Values must be the same length.");
            }

            var zipped = keys.Zip(values, (k, v) => new KeyValuePair<string, object>(k, v));
            AssertMatch(feature, expected, new RouteValueDictionary(zipped));
        }

        public static void AssertMatch(
            IEndpointFeature feature,
            Endpoint expected,
            RouteValueDictionary values,
            bool ignoreValues = false)
        {
            if (feature.Endpoint == null)
            {
                throw new XunitException($"Was expected to match '{expected.DisplayName}' but did not match.");
            }

            if (feature.Values == null)
            {
                throw new XunitException("Values is null.");
            }

            if (!object.ReferenceEquals(expected, feature.Endpoint))
            {
                throw new XunitException(
                    $"Was expected to match '{expected.DisplayName}' but matched " +
                    $"'{feature.Endpoint.DisplayName}' with values: {FormatRouteValues(feature.Values)}.");
            }

            if (!ignoreValues)
            {
                // Note: this comparison is intended for unit testing, and is stricter than necessary to make tests
                // more precise. Route value comparisons in product code are more flexible than a simple .Equals.
                if (values.Count != feature.Values.Count ||
                    !values.OrderBy(kvp => kvp.Key).SequenceEqual(feature.Values.OrderBy(kvp => kvp.Key)))
                {
                    throw new XunitException(
                        $"Was expected to match '{expected.DisplayName}' with values {FormatRouteValues(values)} but matched " +
                        $"values: {FormatRouteValues(feature.Values)}.");
                }
            }
        }

        public static void AssertNotMatch(IEndpointFeature feature)
        {
            if (feature.Endpoint != null)
            {
                throw new XunitException(
                    $"Was expected not to match '{feature.Endpoint.DisplayName}' " +
                    $"but matched with values: {FormatRouteValues(feature.Values)}.");
            }
        }

        private static string FormatRouteValues(RouteValueDictionary values)
        {
            return "{" + string.Join(", ", values.Select(kvp => $"{kvp.Key} = '{kvp.Value}'")) + "}";
        }
    }
}

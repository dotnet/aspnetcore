// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal static class DispatcherAssert
    {
        public static void AssertMatch(IEndpointFeature feature, Endpoint expected)
        {
            AssertMatch(feature, expected, new RouteValueDictionary());
        }

        public static void AssertMatch(IEndpointFeature feature, Endpoint expected, RouteValueDictionary values)
        {
            if (feature.Endpoint == null)
            {
                throw new XunitException($"Was expected to match '{expected.DisplayName}' but did not match.");
            }

            if (!object.ReferenceEquals(expected, feature.Endpoint))
            {
                throw new XunitException(
                    $"Was expected to match '{expected.DisplayName}' but matched " +
                    $"'{feature.Endpoint.DisplayName}' with values: {FormatRouteValues(feature.Values)}.");
            }

            if (values.Count != feature.Values.Count ||
                !values.OrderBy(kvp => kvp.Key).SequenceEqual(feature.Values.OrderBy(kvp => kvp.Key)))
            {
                throw new XunitException(
                    $"Was expected to match '{expected.DisplayName}' with values {FormatRouteValues(values)} but matched " +
                    $"values: {FormatRouteValues(feature.Values)}.");
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

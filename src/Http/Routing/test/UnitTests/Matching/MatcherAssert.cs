// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Routing.Matching;

internal static class MatcherAssert
{
    public static void AssertRouteValuesEqual(object expectedValues, RouteValueDictionary actualValues)
    {
        AssertRouteValuesEqual(new RouteValueDictionary(expectedValues), actualValues);
    }

    public static void AssertRouteValuesEqual(RouteValueDictionary expectedValues, RouteValueDictionary actualValues)
    {
        if (expectedValues.Count != actualValues.Count ||
            !expectedValues.OrderBy(kvp => kvp.Key).SequenceEqual(actualValues.OrderBy(kvp => kvp.Key)))
        {
            throw new XunitException(
                $"Expected values:{FormatRouteValues(expectedValues)} Actual values: {FormatRouteValues(actualValues)}.");
        }
    }

    public static void AssertMatch(HttpContext httpContext, Endpoint expected)
    {
        AssertMatch(httpContext, expected, new RouteValueDictionary());
    }

    public static void AssertMatch(HttpContext httpContext, Endpoint expected, bool ignoreValues)
    {
        AssertMatch(httpContext, expected, new RouteValueDictionary(), ignoreValues);
    }

    public static void AssertMatch(HttpContext httpContext, Endpoint expected, object values)
    {
        AssertMatch(httpContext, expected, new RouteValueDictionary(values));
    }

    public static void AssertMatch(HttpContext httpContext, Endpoint expected, string[] keys, string[] values)
    {
        keys = keys ?? Array.Empty<string>();
        values = values ?? Array.Empty<string>();

        if (keys.Length != values.Length)
        {
            throw new XunitException("Keys and Values must be the same length.");
        }

        var zipped = keys.Zip(values, (k, v) => new KeyValuePair<string, object>(k, v));
        AssertMatch(httpContext, expected, new RouteValueDictionary(zipped));
    }

    public static void AssertMatch(
        HttpContext httpContext,
        Endpoint expected,
        RouteValueDictionary values,
        bool ignoreValues = false)
    {
        if (httpContext.GetEndpoint() == null)
        {
            throw new XunitException($"Was expected to match '{expected.DisplayName}' but did not match.");
        }

        var actualValues = httpContext.Request.RouteValues;

        if (actualValues == null)
        {
            throw new XunitException("RouteValues is null.");
        }

        if (!object.ReferenceEquals(expected, httpContext.GetEndpoint()))
        {
            throw new XunitException(
                $"Was expected to match '{expected.DisplayName}' but matched " +
                $"'{httpContext.GetEndpoint().DisplayName}' with values: {FormatRouteValues(actualValues)}.");
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

    public static void AssertNotMatch(HttpContext httpContext)
    {
        if (httpContext.GetEndpoint() != null)
        {
            throw new XunitException(
                $"Was expected not to match '{httpContext.GetEndpoint().DisplayName}' " +
                $"but matched with values: {FormatRouteValues(httpContext.Request.RouteValues)}.");
        }
    }

    private static string FormatRouteValues(RouteValueDictionary values)
    {
        return values == null ? "{}" : "{" + string.Join(", ", values.Select(kvp => $"{kvp.Key} = '{kvp.Value}'")) + "}";
    }
}

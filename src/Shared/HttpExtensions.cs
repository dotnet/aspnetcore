// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

internal static class HttpExtensions
{
    internal const string UrlEncodedFormContentType = "application/x-www-form-urlencoded";
    internal const string MultipartFormContentType = "multipart/form-data";

    internal static bool IsValidHttpMethodForForm(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method);

    // Key is a string so shared code works across different assemblies (hosting, error handling middleware, etc).
    internal const string OriginalEndpointKey = "__OriginalEndpoint";

    internal static bool IsValidContentTypeForForm(string? contentType)
    {
        if (contentType == null)
        {
            return false;
        }

        // Abort early if this doesn't look like it could be a form-related content-type

        if (contentType.Length < MultipartFormContentType.Length)
        {
            return false;
        }

        return contentType.Equals(UrlEncodedFormContentType, StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith(MultipartFormContentType, StringComparison.OrdinalIgnoreCase);
    }

    internal static Endpoint? GetOriginalEndpoint(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        // Some middleware re-execute the middleware pipeline with the HttpContext. Before they do this, they clear state from context, such as the previously matched endpoint.
        // The original endpoint is stashed with a known key in HttpContext.Items. Use it as a fallback.
        if (endpoint == null && context.Items.TryGetValue(OriginalEndpointKey, out var e) && e is Endpoint originalEndpoint)
        {
            endpoint = originalEndpoint;
        }
        return endpoint;
    }

    internal static void ClearEndpoint(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            context.Items[OriginalEndpointKey] = endpoint;

            // An endpoint may have already been set. Since we're going to re-invoke the middleware pipeline we need to reset
            // the endpoint and route values to ensure things are re-calculated.
            context.SetEndpoint(endpoint: null);
        }

        var routeValuesFeature = context.Features.Get<IRouteValuesFeature>();
        if (routeValuesFeature != null)
        {
            routeValuesFeature.RouteValues = null!;
        }
    }
}

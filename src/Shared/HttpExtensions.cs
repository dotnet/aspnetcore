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

    internal const string ClearedEndpointKey = "__ClearedEndpoint";

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

    internal static void ClearEndpoint(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            context.Items[ClearedEndpointKey] = endpoint;

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

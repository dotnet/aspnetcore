// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Microsoft.AspNetCore.Components.Hosting;

internal sealed class NavigationHttpContextStartupValueProvider : IHttpContextStartupValueProvider
{
    internal const string BaseUriKey = "document.baseURI";
    internal const string LocationHrefKey = "location.href";

    public IReadOnlyDictionary<string, string> GetValues(HttpContext httpContext)
        => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [BaseUriKey] = GetContextBaseUri(httpContext.Request),
            [LocationHrefKey] = GetFullUri(httpContext.Request),
        };

    internal static string GetFullUri(HttpRequest request)
        => UriHelper.BuildAbsolute(
            request.Scheme,
            request.Host,
            request.PathBase,
            request.Path,
            request.QueryString);

    internal static string GetContextBaseUri(HttpRequest request)
    {
        var result = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase);

        return result.EndsWith('/') ? result : result += "/";
    }
}

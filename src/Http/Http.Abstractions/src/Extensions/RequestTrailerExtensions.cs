// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// HttpRequest extensions for working with request trailing headers.
/// </summary>
public static class RequestTrailerExtensions
{
    /// <summary>
    /// Gets the request "Trailer" header that lists which trailers to expect after the body.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static StringValues GetDeclaredTrailers(this HttpRequest request)
    {
        return request.Headers.GetCommaSeparatedValues(HeaderNames.Trailer);
    }

    /// <summary>
    /// Indicates if the request supports receiving trailer headers.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static bool SupportsTrailers(this HttpRequest request)
    {
        return request.HttpContext.Features.Get<IHttpRequestTrailersFeature>() != null;
    }

    /// <summary>
    /// Checks if the request supports trailers and they are available to be read now.
    /// This does not mean that there are any trailers to read.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static bool CheckTrailersAvailable(this HttpRequest request)
    {
        return request.HttpContext.Features.Get<IHttpRequestTrailersFeature>()?.Available == true;
    }

    /// <summary>
    /// Gets the requested trailing header from the response. Check <see cref="SupportsTrailers"/>
    /// or a NotSupportedException may be thrown.
    /// Check <see cref="CheckTrailersAvailable" /> or an InvalidOperationException may be thrown.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="trailerName"></param>
    public static StringValues GetTrailer(this HttpRequest request, string trailerName)
    {
        var feature = request.HttpContext.Features.Get<IHttpRequestTrailersFeature>();
        if (feature == null)
        {
            throw new NotSupportedException("This request does not support trailers.");
        }

        return feature.Trailers[trailerName];
    }
}

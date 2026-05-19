// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Extensions;

/// <summary>
/// Extension methods for working with multipart form requests.
/// </summary>
public static class HttpRequestMultipartExtensions
{
    /// <summary>
    /// Gets the multipart boundary from the <c>Content-Type</c> header.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <returns>The multipart boundary.</returns>
    public static string GetMultipartBoundary(this HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaType))
        {
            return string.Empty;
        }
        return HeaderUtilities.RemoveQuotes(mediaType.Boundary).ToString();
    }
}

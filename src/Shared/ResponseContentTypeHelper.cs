// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Internal;

internal static class ResponseContentTypeHelper
{
    /// <summary>
    /// Gets the content type and encoding that need to be used for the response.
    /// The priority for selecting the content type is:
    /// 1. ContentType property set on the action result
    /// 2. <see cref="HttpResponse.ContentType"/> property set on <see cref="HttpResponse"/>
    /// 3. Default content type set on the action result
    /// </summary>
    /// <remarks>
    /// The user supplied content type is not modified and is used as is. For example, if user
    /// sets the content type to be "text/plain" without any encoding, then the default content type's
    /// encoding is used to write the response and the ContentType header is set to be "text/plain" without any
    /// "charset" information.
    /// </remarks>
    public static void ResolveContentTypeAndEncoding(
        string? actionResultContentType,
        string? httpResponseContentType,
        (string defaultContentType, Encoding defaultEncoding) @default,
        Func<string, Encoding?> getEncoding,
        out string resolvedContentType,
        out Encoding resolvedContentTypeEncoding)
    {
        var (defaultContentType, defaultContentTypeEncoding) = @default;

        // 1. User sets the ContentType property on the action result
        if (actionResultContentType != null)
        {
            resolvedContentType = actionResultContentType;
            var actionResultEncoding = getEncoding(actionResultContentType);
            resolvedContentTypeEncoding = actionResultEncoding ?? defaultContentTypeEncoding;
            return;
        }

        // 2. User sets the ContentType property on the http response directly
        if (!string.IsNullOrEmpty(httpResponseContentType))
        {
            var mediaTypeEncoding = getEncoding(httpResponseContentType);
            if (mediaTypeEncoding != null)
            {
                resolvedContentType = httpResponseContentType;
                resolvedContentTypeEncoding = mediaTypeEncoding;
            }
            else
            {
                resolvedContentType = httpResponseContentType;
                resolvedContentTypeEncoding = defaultContentTypeEncoding;
            }

            return;
        }

        // 3. Fall-back to the default content type
        resolvedContentType = defaultContentType;
        resolvedContentTypeEncoding = defaultContentTypeEncoding;
    }

    public static Encoding? GetEncoding(string mediaType)
    {
        if (MediaTypeHeaderValue.TryParse(mediaType, out var parsed))
        {
            return parsed.Encoding;
        }

        return default;
    }
}

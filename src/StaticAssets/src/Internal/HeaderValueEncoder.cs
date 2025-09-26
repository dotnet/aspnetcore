// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.StaticAssets.Internal;

internal static class HeaderValueEncoder
{
    public static string Sanitize(string headerName, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (string.Equals(headerName, HeaderNames.Link, StringComparison.OrdinalIgnoreCase))
        {
            return EncodeLinkHeaderValue(value);
        }

        if (string.Equals(headerName, HeaderNames.Location, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(headerName, HeaderNames.ContentLocation, StringComparison.OrdinalIgnoreCase))
        {
            return EncodePotentialRelativeUrl(value);
        }

        // For all other headers, leave as-is.
        return value;
    }

    // Encodes non-ASCII in <...> targets while preserving link-params.
    // Supports multiple link-values in a single header value.
    internal static string EncodeLinkHeaderValue(string value)
    {
        var sb = new StringBuilder(value.Length);
        var idx = 0;

        while (idx < value.Length)
        {
            var lt = value.IndexOf('<', idx);
            if (lt < 0)
            {
                sb.Append(value, idx, value.Length - idx);
                break;
            }

            var gt = value.IndexOf('>', lt + 1);
            if (gt < 0)
            {
                sb.Append(value, idx, value.Length - idx);
                break;
            }

            // Copy any prefix before '<'
            sb.Append(value, idx, lt - idx);

            var url = value.Substring(lt + 1, gt - lt - 1);
            var encodedUrl = EncodePotentialRelativeUrl(url);

            sb.Append('<').Append(encodedUrl).Append('>');

            // Continue after '>'
            idx = gt + 1;
        }

        return sb.ToString();
    }

    // If the input is a relative URL, percent-encode non-ASCII safely using PathString.
    // If absolute, return as-is to avoid altering hosts/schemes (expected to already be ASCII/IDNA-encoded).
    internal static string EncodePotentialRelativeUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        // Check if it's truly an absolute URL with http/https scheme (not file:// or relative paths)
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
            !string.IsNullOrEmpty(uri.Scheme) && 
            uri.Scheme != "file")
        {
            return url; // Assume caller provided an ASCII-safe absolute URI.
        }

        if (url[0] == '/')
        {
            return new PathString(url).ToUriComponent();
        }

        // Relative reference without leading slash; encode as a segment.
        var encoded = new PathString("/" + url).ToUriComponent();
        return encoded.Length > 0 && encoded[0] == '/' ? encoded.Substring(1) : encoded;
    }
}
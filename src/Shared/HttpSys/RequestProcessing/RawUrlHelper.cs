// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HttpSys.Internal;

internal static class RawUrlHelper
{
    /// <summary>
    /// Find the segment of the URI byte array which represents the path.
    /// </summary>
    public static Span<byte> GetPath(Span<byte> raw)
    {
        // performance
        var pathStartIndex = 0;

        // Performance improvement: accept two cases upfront
        //
        // 1) Since nearly all strings are relative Uris, just look if the string starts with '/'.
        // If so, we have a relative Uri and the path starts at position 0.
        // (http.sys already trimmed leading whitespaces)
        //
        // 2) The URL is simply '*'
        if (raw[0] != '/' && !(raw.Length == 1 && raw[0] == '*'))
        {
            // We can't check against cookedUriScheme, since http.sys allows for request http://myserver/ to
            // use a request line 'GET https://myserver/' (note http vs. https). Therefore check if the
            // Uri starts with either http:// or https://.
            var authorityStartIndex = FindHttpOrHttps(raw);
            if (authorityStartIndex > 0)
            {
                // we have an absolute Uri. Find out where the authority ends and the path begins.
                // Note that Uris like "http://server?query=value/1/2" are invalid according to RFC2616
                // and http.sys behavior: If the Uri contains a query, there must be at least one '/'
                // between the authority and the '?' character: It's safe to just look for the first
                // '/' after the authority to determine the beginning of the path.
                pathStartIndex = Find(raw, authorityStartIndex, (byte)'/');
                if (pathStartIndex == -1)
                {
                    // e.g. for request lines like: 'GET http://myserver' (no final '/')
                    // At this point we can return a path with a slash.
                    return default;
                }
            }
            else
            {
                // RFC2616: Request-URI = "*" | absoluteURI | abs_path | authority
                // 'authority' can only be used with CONNECT which is never received by HttpListener.
                // I.e. if we don't have an absolute path (must start with '/') and we don't have
                // an absolute Uri (must start with http:// or https://), then 'uriString' must be '*'.
                throw new InvalidOperationException("Invalid URI format");
            }
        }

        // Find end of path: The path is terminated by
        // - the first '?' character
        // - the first '#' character: This is never the case here, since http.sys won't accept
        //   Uris containing fragments. Also, RFC2616 doesn't allow fragments in request Uris.
        // - end of Uri string
        var scan = pathStartIndex + 1;
        while (scan < raw.Length && raw[scan] != '?')
        {
            scan++;
        }

        return raw.Slice(pathStartIndex, scan - pathStartIndex);
    }

    /// <summary>
    /// Compare the beginning portion of the raw URL byte array to https:// and http://
    /// </summary>
    /// <param name="raw">The byte array represents the raw URI</param>
    /// <returns>Length of the matched bytes, 0 if it is not matched.</returns>
    private static int FindHttpOrHttps(Span<byte> raw)
    {
        if (raw.Length < 7)
        {
            return 0;
        }

        if (raw[0] != 'h' && raw[0] != 'H')
        {
            return 0;
        }

        if (raw[1] != 't' && raw[1] != 'T')
        {
            return 0;
        }

        if (raw[2] != 't' && raw[2] != 'T')
        {
            return 0;
        }

        if (raw[3] != 'p' && raw[3] != 'P')
        {
            return 0;
        }

        if (raw[4] == ':')
        {
            if (raw[5] != '/' || raw[6] != '/')
            {
                return 0;
            }
            else
            {
                return 7;
            }
        }
        else if (raw[4] == 's' || raw[4] == 'S')
        {
            if (raw.Length < 8)
            {
                return 0;
            }

            if (raw[5] != ':' || raw[6] != '/' || raw[7] != '/')
            {
                return 0;
            }
            else
            {
                return 8;
            }
        }
        else
        {
            return 0;
        }
    }

    private static int Find(Span<byte> raw, int begin, byte target)
    {
        var idx = raw.Slice(begin).IndexOf(target);
        if (idx != -1)
        {
            return idx + begin;
        }
        return idx;
    }
}

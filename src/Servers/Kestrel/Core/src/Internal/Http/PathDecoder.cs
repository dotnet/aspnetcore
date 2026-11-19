// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

internal static class PathDecoder
{
    public static string DecodePath(Span<byte> path, bool pathEncoded, string rawTarget, int queryLength)
    {
        int pathLength;
        if (pathEncoded)
        {
            // URI was encoded, unescape and then parse as UTF-8
            pathLength = UrlDecoder.DecodeInPlace(path, isFormEncoding: false);

            // Backslashes decoded from percent-encoding (e.g. %5C) must be normalized to forward
            // slashes before dot segments are removed or the path is handed to any other code that
            // looks for '/' as the segment separator.
            PathNormalizer.NormalizeBackslashes(path.Slice(0, pathLength));

            // Removing dot segments must be done after unescaping. From RFC 3986:
            //
            // URI producing applications should percent-encode data octets that
            // correspond to characters in the reserved set unless these characters
            // are specifically allowed by the URI scheme to represent data in that
            // component.  If a reserved character is found in a URI component and
            // no delimiting role is known for that character, then it must be
            // interpreted as representing the data octet corresponding to that
            // character's encoding in US-ASCII.
            //
            // https://tools.ietf.org/html/rfc3986#section-2.2
            pathLength = PathNormalizer.RemoveDotSegments(path.Slice(0, pathLength));

            return Encoding.UTF8.GetString(path.Slice(0, pathLength));
        }

        // The target may contain a literal (not percent-encoded) backslash even though no
        // percent-encoding was present at all, so this also needs normalizing.
        var containedBackslash = PathNormalizer.NormalizeBackslashes(path);
        pathLength = PathNormalizer.RemoveDotSegments(path);

        if (!containedBackslash && path.Length == pathLength && queryLength == 0)
        {
            // If no decoding or backslash normalization was required, no dot segments were
            // removed and there is no query, the request path is the same as the raw target
            return rawTarget;
        }

        return path.Slice(0, pathLength).GetAsciiString();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

internal static class PathNormalizer
{
    private const byte ByteSlash = (byte)'/';
    private const byte ByteDot = (byte)'.';

    public static string DecodePath(Span<byte> path, bool pathEncoded, string rawTarget, int queryLength)
    {
        int pathLength;
        if (pathEncoded)
        {
            // URI was encoded, unescape and then parse as UTF-8
            pathLength = UrlDecoder.DecodeInPlace(path, isFormEncoding: false);

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
            pathLength = RemoveDotSegments(path.Slice(0, pathLength));

            return Encoding.UTF8.GetString(path.Slice(0, pathLength));
        }

        pathLength = RemoveDotSegments(path);

        if (path.Length == pathLength && queryLength == 0)
        {
            // If no decoding was required, no dot segments were removed and
            // there is no query, the request path is the same as the raw target
            return rawTarget;
        }

        return path.Slice(0, pathLength).GetAsciiStringNonNullCharacters();
    }

    // In-place implementation of the algorithm from https://tools.ietf.org/html/rfc3986#section-5.2.4
    public static int RemoveDotSegments(Span<byte> src)
    {
        Debug.Assert(src[0] == '/', "Path segment must always start with a '/'");
        ReadOnlySpan<byte> slashDot = "/."u8;
        ReadOnlySpan<byte> dotSlash = "./"u8;
        if (!ContainsDotSegments(src))
        {
            return src.Length;
        }

        var writtenLength = 0;
        var dst = src;

        while (src.Length > 0)
        {
            var nextSlashDotIndex = src.IndexOf(slashDot);
            if (nextSlashDotIndex < 0)
            {
                // Copy the remianing src to dst, and return.
                src.CopyTo(dst[writtenLength..]);
                writtenLength += src.Length;
                return writtenLength;
            }
            else
            {
                src.Slice(0, nextSlashDotIndex).CopyTo(dst[writtenLength..]);
                writtenLength += nextSlashDotIndex;
                src = src[(nextSlashDotIndex + 2)..];
            }

            switch (src.Length)
            {
                case 0: // Ending with /.
                    dst[writtenLength++] = ByteSlash;
                    return writtenLength;
                case 1: // Ending with either /.. or /./
                    if (src[0] == ByteDot)
                    {
                        // Backtrack
                        if (writtenLength > 0)
                        {
                            var lastIndex = dst.Slice(0, writtenLength - 1).LastIndexOf(ByteSlash);
                            if (lastIndex < 0)
                            {
                                writtenLength = 0;
                            }
                            else
                            {
                                writtenLength = lastIndex + 1;
                            }
                            return writtenLength;
                        }
                        else
                        {
                            dst[0] = ByteSlash;
                            return 1;
                        }
                    }
                    else if (src[0] == ByteSlash)
                    {
                        dst[writtenLength++] = ByteSlash;
                        return writtenLength;
                    }
                    break;
                default: // Case of /../ or /./
                    if (dotSlash.SequenceEqual(src.Slice(0, 2)))
                    {
                        // Backtrack
                        if (writtenLength > 0)
                        {
                            var lastIndex = dst.Slice(0, writtenLength - 1).LastIndexOf(ByteSlash);
                            if (lastIndex < 0)
                            {
                                writtenLength = 0;
                            }
                            else
                            {
                                writtenLength = lastIndex;
                            }
                        }
                        src = src.Slice(1);
                    }
                    else if (src[0] == ByteSlash && writtenLength > 0)
                    {
                        //dst[writtenLength++] = ByteSlash;
                    }
                    break;
            }
        }
        return writtenLength;
    }

    public static bool ContainsDotSegments(Span<byte> src)
    {
        Debug.Assert(src[0] == '/', "Path segment must always start with a '/'");
        ReadOnlySpan<byte> slashDot = "/."u8;
        ReadOnlySpan<byte> dotSlash = "./"u8;
        while (src.Length > 0)
        {
            var nextSlashDotIndex = src.IndexOf(slashDot);
            if (nextSlashDotIndex < 0)
            {
                return false;
            }
            else
            {
                src = src[(nextSlashDotIndex + 2)..];
            }
            switch (src.Length)
            {
                case 0: // Case of /.
                    return true;
                case 1: // Case of /.. or /./
                    if (src[0] == ByteDot || src[0] == ByteSlash)
                    {
                        return true;
                    }
                    break;
                default: // Case of /../ or /./ 
                    if (dotSlash.SequenceEqual(src.Slice(0, 2)) || src[0] == ByteSlash)
                    {
                        return true;
                    }
                    break;
            }
        }
        return false;
    }
}

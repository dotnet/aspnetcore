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
        ReadOnlySpan<byte> dotSlash = "./"u8;
        ReadOnlySpan<byte> slashDot = "/."u8;

        var writtenLength = 0;
        var dst = src;

        while (src.Length > 0)
        {
            var nextDotSegmentIndex = src.IndexOf(slashDot);
            if (nextDotSegmentIndex < 0)
            {
                // Copy the remianing src to dst, and return.
                src.CopyTo(dst[writtenLength..]);
                writtenLength += src.Length;
                return writtenLength;
            }
            else if (nextDotSegmentIndex > 0)
            {
                // Copy until the next segment excluding the trailer. Move the read pointer
                // beyond the initial /. section, because FirstIndexOfDotSegment return the
                // index of a complete dot segment.
                src.Slice(0, nextDotSegmentIndex).CopyTo(dst[writtenLength..]);
                writtenLength += nextDotSegmentIndex;
                src = src[(nextDotSegmentIndex)..];
            }

            switch (src.Length)
            {
                case 0:
                case 1:
                    Debug.Fail("This should be always larger than 1");
                    break;
                case 2: // Ending with /.
                    dst[writtenLength++] = ByteSlash;
                    return writtenLength;

                case 3: // Ending with /.. or /./
                    if (src[2] == ByteDot)
                    {
                        // Remove the last segment and replace the path with /
                        var lastSlashIndex = dst.Slice(0, writtenLength).LastIndexOf(ByteSlash);

                        // If this was the beginning of the string, then return /
                        if (lastSlashIndex < 0)
                        {
                            dst[0] = ByteSlash;
                            return 1;
                        }
                        else
                        {
                            writtenLength = lastSlashIndex + 1;
                        }
                        return writtenLength;
                    }
                    else if (src[2] == ByteSlash)
                    {
                        // Replace the /./ segment with a closing /
                        dst[writtenLength++] = ByteSlash;
                        return writtenLength;
                    }
                    else
                    {
                        dst[writtenLength++] = ByteSlash;
                        src = src.Slice(1);
                    }
                    break;
                default: // Case of /../ or /./
                    if (dotSlash.SequenceEqual(src.Slice(2, 2)))
                    {
                        // Remove the last segment and replace the path with /
                        var lastIndex = dst.Slice(0, writtenLength).LastIndexOf(ByteSlash);

                        // Move write pointer to the end of the previous segment without / or to start position
                        writtenLength = Math.Max(0, lastIndex);

                        // Move the read pointer to the next segments beginning including /
                        src = src.Slice(3);
                    }
                    else if (src[2] == ByteSlash)
                    {
                        src = src.Slice(2);
                    }
                    else
                    {
                        dst[writtenLength++] = ByteSlash;
                        dst[writtenLength++] = ByteDot;
                        src = src.Slice(2);
                    }

                    break;
            }
        }
        return writtenLength;
    }

    public static bool ContainsDotSegments(Span<byte> src)
    {
        return FirstIndexOfDotSegment(src) > -1;
    }

    private static int FirstIndexOfDotSegment(Span<byte> src)
    {
        Debug.Assert(src[0] == '/', "Path segment must always start with a '/'");
        ReadOnlySpan<byte> slashDot = "/."u8;
        ReadOnlySpan<byte> dotSlash = "./"u8;
        int totalLength = 0;
        while (src.Length > 0)
        {
            var nextSlashDotIndex = src.IndexOf(slashDot);
            if (nextSlashDotIndex < 0)
            {
                return -1;
            }
            else
            {
                src = src[(nextSlashDotIndex + 2)..];
                totalLength += nextSlashDotIndex + 2;
            }
            switch (src.Length)
            {
                case 0: // Case of /.
                    return totalLength - 2;
                case 1: // Case of /.. or /./
                    if (src[0] == ByteDot || src[0] == ByteSlash)
                    {
                        return totalLength - 2;
                    }
                    break;
                default: // Case of /../ or /./ 
                    if (dotSlash.SequenceEqual(src.Slice(0, 2)) || src[0] == ByteSlash)
                    {
                        return totalLength - 2;
                    }
                    break;
            }
        }
        return -1;
    }
}

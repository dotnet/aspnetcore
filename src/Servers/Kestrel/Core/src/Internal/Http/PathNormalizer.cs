// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
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
        var readPointer = 0;

        while (src.Length > readPointer)
        {
            var currentSrc = src[readPointer..];
            var nextDotSegmentIndex = currentSrc.IndexOf(slashDot);
            if (nextDotSegmentIndex < 0 && readPointer == 0)
            {
                return src.Length;
            }
            if (nextDotSegmentIndex < 0)
            {
                // Copy the remianing src to dst, and return.
                currentSrc.CopyTo(src[writtenLength..]);
                writtenLength += src.Length - readPointer;
                return writtenLength;
            }
            else if (nextDotSegmentIndex > 0)
            {
                // Copy until the next segment excluding the trailer.
                currentSrc[..nextDotSegmentIndex].CopyTo(src[writtenLength..]);
                writtenLength += nextDotSegmentIndex;
                readPointer += nextDotSegmentIndex;
            }

            var remainingLength = src.Length - readPointer;

            // Case of /../ or /./ or non-dot segments.
            if (remainingLength > 3)
            {
                var nextIndex = readPointer + 2;

                if (src[nextIndex] == ByteSlash)
                {
                    // Case: /./
                    readPointer = nextIndex;
                }
                else if (MemoryMarshal.CreateSpan(ref src[nextIndex], 2).StartsWith(dotSlash))
                {
                    // Case: /../
                    // Remove the last segment and replace the path with /
                    var lastIndex = MemoryMarshal.CreateSpan(ref src[0], writtenLength).LastIndexOf(ByteSlash);

                    // Move write pointer to the end of the previous segment without / or to start position
                    writtenLength = int.Max(0, lastIndex);

                    // Move the read pointer to the next segments beginning including /
                    readPointer += 3;
                }
                else
                {
                    // Not a dot segment e.g. /.a, copy the matched /. and the next character then bump the read pointer
                    src.Slice(readPointer, 3).CopyTo(src[writtenLength..]);
                    writtenLength += 3;
                    readPointer = nextIndex + 1;
                }
            }

            // Ending with /.. or /./ or non-dot segments.
            else if (remainingLength == 3)
            {
                var nextIndex = readPointer + 2;
                if (src[nextIndex] == ByteSlash)
                {
                    // Case: /./ Replace the /./ segment with a closing /
                    src[writtenLength++] = ByteSlash;
                    return writtenLength;
                }
                else if (src[nextIndex] == ByteDot)
                {
                    // Case: /.. Remove the last segment and replace the path with /
                    var lastSlashIndex = MemoryMarshal.CreateSpan(ref src[0], writtenLength).LastIndexOf(ByteSlash);

                    // If this was the beginning of the string, then return /
                    if (lastSlashIndex < 0)
                    {
                        Debug.Assert(src[0] == '/');
                        return 1;
                    }
                    else
                    {
                        writtenLength = lastSlashIndex + 1;
                    }
                    return writtenLength;
                }
                else
                {
                    // Not a dot segment e.g. /.a, copy the remaining part.
                    src[readPointer..].CopyTo(src[writtenLength..]);
                    return writtenLength + 3;
                }
            }
            // Ending with /.
            else if (remainingLength == 2)
            {
                src[writtenLength++] = ByteSlash;
                return writtenLength;
            }
        }
        return writtenLength;
    }
}

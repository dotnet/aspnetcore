// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Internal;

internal static class PathNormalizer
{
    private const byte ByteSlash = (byte)'/';
    private const byte ByteDot = (byte)'.';

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

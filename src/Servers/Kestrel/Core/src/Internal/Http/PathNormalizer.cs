// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
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
        public static unsafe int RemoveDotSegments(Span<byte> input)
        {
            fixed (byte* start = input)
            {
                var end = start + input.Length;
                return RemoveDotSegments(start, end);
            }
        }

        public static unsafe int RemoveDotSegments(byte* start, byte* end)
        {
            if (!ContainsDotSegments(start, end))
            {
                return (int)(end - start);
            }

            var src = start;
            var dst = start;

            while (src < end)
            {
                var ch1 = *src;
                Debug.Assert(ch1 == '/', "Path segment must always start with a '/'");

                byte ch2, ch3, ch4;

                switch (end - src)
                {
                    case 1:
                        break;
                    case 2:
                        ch2 = *(src + 1);

                        if (ch2 == ByteDot)
                        {
                            // B.  if the input buffer begins with a prefix of "/./" or "/.",
                            //     where "." is a complete path segment, then replace that
                            //     prefix with "/" in the input buffer; otherwise,
                            src += 1;
                            *src = ByteSlash;
                            continue;
                        }

                        break;
                    case 3:
                        ch2 = *(src + 1);
                        ch3 = *(src + 2);

                        if (ch2 == ByteDot && ch3 == ByteDot)
                        {
                            // C.  if the input buffer begins with a prefix of "/../" or "/..",
                            //     where ".." is a complete path segment, then replace that
                            //     prefix with "/" in the input buffer and remove the last
                            //     segment and its preceding "/" (if any) from the output
                            //     buffer; otherwise,
                            src += 2;
                            *src = ByteSlash;

                            if (dst > start)
                            {
                                do
                                {
                                    dst--;
                                } while (dst > start && *dst != ByteSlash);
                            }

                            continue;
                        }
                        else if (ch2 == ByteDot && ch3 == ByteSlash)
                        {
                            // B.  if the input buffer begins with a prefix of "/./" or "/.",
                            //     where "." is a complete path segment, then replace that
                            //     prefix with "/" in the input buffer; otherwise,
                            src += 2;
                            continue;
                        }

                        break;
                    default:
                        ch2 = *(src + 1);
                        ch3 = *(src + 2);
                        ch4 = *(src + 3);

                        if (ch2 == ByteDot && ch3 == ByteDot && ch4 == ByteSlash)
                        {
                            // C.  if the input buffer begins with a prefix of "/../" or "/..",
                            //     where ".." is a complete path segment, then replace that
                            //     prefix with "/" in the input buffer and remove the last
                            //     segment and its preceding "/" (if any) from the output
                            //     buffer; otherwise,
                            src += 3;

                            if (dst > start)
                            {
                                do
                                {
                                    dst--;
                                } while (dst > start && *dst != ByteSlash);
                            }

                            continue;
                        }
                        else if (ch2 == ByteDot && ch3 == ByteSlash)
                        {
                            // B.  if the input buffer begins with a prefix of "/./" or "/.",
                            //     where "." is a complete path segment, then replace that
                            //     prefix with "/" in the input buffer; otherwise,
                            src += 2;
                            continue;
                        }

                        break;
                }

                // E.  move the first path segment in the input buffer to the end of
                //     the output buffer, including the initial "/" character (if
                //     any) and any subsequent characters up to, but not including,
                //     the next "/" character or the end of the input buffer.
                do
                {
                    *dst++ = ch1;
                    ch1 = *++src;
                } while (src < end && ch1 != ByteSlash);
            }

            if (dst == start)
            {
                *dst++ = ByteSlash;
            }

            return (int)(dst - start);
        }

        public static unsafe bool ContainsDotSegments(byte* start, byte* end)
        {
            var src = start;
            while (src < end)
            {
                var ch1 = *src;
                Debug.Assert(ch1 == '/', "Path segment must always start with a '/'");

                byte ch2, ch3, ch4;

                switch (end - src)
                {
                    case 1:
                        break;
                    case 2:
                        ch2 = *(src + 1);

                        if (ch2 == ByteDot)
                        {
                            return true;
                        }

                        break;
                    case 3:
                        ch2 = *(src + 1);
                        ch3 = *(src + 2);

                        if ((ch2 == ByteDot && ch3 == ByteDot) ||
                            (ch2 == ByteDot && ch3 == ByteSlash))
                        {
                            return true;
                        }

                        break;
                    default:
                        ch2 = *(src + 1);
                        ch3 = *(src + 2);
                        ch4 = *(src + 3);

                        if ((ch2 == ByteDot && ch3 == ByteDot && ch4 == ByteSlash) ||
                            (ch2 == ByteDot && ch3 == ByteSlash))
                        {
                            return true;
                        }

                        break;
                }

                do
                {
                    ch1 = *++src;
                } while (src < end && ch1 != ByteSlash);
            }

            return false;
        }
    }
}

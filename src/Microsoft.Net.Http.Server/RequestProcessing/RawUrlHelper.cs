// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

// -----------------------------------------------------------------------
// <copyright file="SslStatus.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Microsoft.Net.Http.Server
{
    internal static class RawUrlHelper
    {
        /// <summary>
        /// Find the segment of the URI byte array which represents the path.
        /// </summary>
        public static ArraySegment<byte> GetPath(byte[] raw)
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
                    pathStartIndex = Find(raw, authorityStartIndex, '/');
                    if (pathStartIndex == -1)
                    {
                        // e.g. for request lines like: 'GET http://myserver' (no final '/')
                        pathStartIndex = raw.Length;
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

            return new ArraySegment<byte>(raw, pathStartIndex, scan - pathStartIndex);
        }

        /// <summary>
        /// Compare the beginning portion of the raw URL byte array to https:// and http://
        /// </summary>
        /// <param name="raw">The byte array represents the raw URI</param>
        /// <returns>Length of the matched bytes, 0 if it is not matched.</returns>
        private static int FindHttpOrHttps(byte[] raw)
        {
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

        private static int Find(byte[] raw, int begin, char target)
        {
            for (var idx = begin; idx < raw.Length; ++idx)
            {
                if (raw[idx] == target)
                {
                    return idx;
                }
            }

            return -1;
        }
    }
}

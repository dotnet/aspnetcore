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
// <copyright file="RequestUriBuilder.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.Net.Http.Server
{
    // We don't use the cooked URL because http.sys unescapes all percent-encoded values. However,
    // we also can't just use the raw Uri, since http.sys supports not only UTF-8, but also ANSI/DBCS and
    // Unicode code points. System.Uri only supports UTF-8.
    // The purpose of this class is to decode all UTF-8 percent encoded characters, with the
    // exception of %2F ('/'), which is left encoded.
    internal sealed class RequestUriBuilder
    {
        private static readonly Encoding Utf8Encoding;

        static RequestUriBuilder()
        {
            Utf8Encoding = new UTF8Encoding(false, true);
        }

        // Process only the path.
        public static string GetRequestPath(byte[] rawUriInBytes, ILogger logger)
        {
            //Debug.Assert(rawUriInBytes == null || rawUriInBytes.Length == 0, "Empty raw URL.");
            //Debug.Assert(logger != null, "Null logger.");

            var rawUriInByte = new UrlInByte(rawUriInBytes);
            var pathInByte = rawUriInByte.Path;

            if (pathInByte.Count == 1 && pathInByte.Array[pathInByte.Offset] == '*')
            {
                return "/*";
            }

            var unescapedRaw = UrlPathDecoder.Unescape(pathInByte);
            return Utf8Encoding.GetString(unescapedRaw.Array, unescapedRaw.Offset, unescapedRaw.Count);
        }
    }
}

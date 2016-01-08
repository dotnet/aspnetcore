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
// <copyright file="HeaderEncoding.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;

namespace Microsoft.Net.Http.Server
{
    internal static class HeaderEncoding
    {
        // It should just be ASCII or ANSI, but they break badly with un-expected values. We use UTF-8 because it's the same for
        // ASCII, and because some old client would send UTF8 Host headers and expect UTF8 Location responses
        // (e.g. IE and HttpWebRequest on intranets).
        private static Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

        internal static unsafe string GetString(sbyte* pBytes, int byteCount)
        {
            // net451: return new string(pBytes, 0, byteCount, Encoding);

            var charCount = Encoding.GetCharCount((byte*)pBytes, byteCount);
            var chars = new char[charCount];
            fixed (char* pChars = chars)
            {
                var count = Encoding.GetChars((byte*)pBytes, byteCount, pChars, charCount);
                System.Diagnostics.Debug.Assert(count == charCount);
            }
            return new string(chars);
        }

        internal static byte[] GetBytes(string myString)
        {
            return Encoding.GetBytes(myString);
        }
    }
}

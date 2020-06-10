// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.HttpSys.Internal
{
    internal static class HeaderEncoding
    {
        private static Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

        internal static unsafe string GetString(byte* pBytes, int byteCount, bool useLatin1)
        {
            if (useLatin1)
            {
                return StringUtilities.GetLatin1StringNonNullCharacters(new Span<byte>(pBytes, byteCount));
            }
            else
            {
                return StringUtilities.GetAsciiOrUTF8StringNonNullCharacters(new Span<byte>(pBytes, byteCount), Encoding);
            }
        }

        internal static byte[] GetBytes(string myString)
        {
            return Encoding.GetBytes(myString);
        }
    }
}

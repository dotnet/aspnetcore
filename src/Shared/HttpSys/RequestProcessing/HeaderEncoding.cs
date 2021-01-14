// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

// Remove once HttpSys has enabled nullable
#nullable enable

namespace Microsoft.AspNetCore.HttpSys.Internal
{
    internal static class HeaderEncoding
    {
        private static readonly Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

        internal static unsafe string GetString(byte* pBytes, int byteCount, bool useLatin1)
        {
            if (useLatin1)
            {
                return new ReadOnlySpan<byte>(pBytes, byteCount).GetLatin1StringNonNullCharacters();
            }
            else
            {
                return new ReadOnlySpan<byte>(pBytes, byteCount).GetAsciiOrUTF8StringNonNullCharacters(Encoding);
            }
        }

        internal static byte[] GetBytes(string myString)
        {
            return Encoding.GetBytes(myString);
        }
    }
}

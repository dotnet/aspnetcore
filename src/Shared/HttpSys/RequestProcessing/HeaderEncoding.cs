// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.HttpSys.Internal
{
    internal static class HeaderEncoding
    {
        private static Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

        internal static unsafe string GetString(byte* pBytes, int byteCount, bool useLatin1)
        {
            if (useLatin1)
            {
                return GetLatin1StringNonNullCharacters(new Span<byte>(pBytes, byteCount));
            }
            else
            {
                return GetAsciiOrUTF8StringNonNullCharacters(new Span<byte>(pBytes, byteCount));
            }
        }

        private static unsafe string GetLatin1StringNonNullCharacters(this Span<byte> span)
        {
            if (span.IsEmpty)
            {
                return string.Empty;
            }

            var resultString = new string('\0', span.Length);

            fixed (char* output = resultString)
            fixed (byte* buffer = span)
            {
                // This returns false if there are any null (0 byte) characters in the string.
                if (!StringUtilities.TryGetLatin1String(buffer, output, span.Length))
                {
                    // null characters are considered invalid
                    throw new InvalidOperationException();
                }
            }

            return resultString;
        }

        private static unsafe string GetAsciiOrUTF8StringNonNullCharacters(this Span<byte> span)
        {
            if (span.IsEmpty)
            {
                return string.Empty;
            }

            var resultString = new string('\0', span.Length);
            fixed (char* output = resultString)
            fixed (byte* buffer = span)
            {
                // This version if AsciiUtilities returns null if there are any null (0 byte) characters
                // StringUtilities.TryGetAsciiString returns null if there are any null (0 byte) characters
                // in the string
                if (!StringUtilities.TryGetAsciiString(buffer, output, span.Length))
                {
                    // null characters are considered invalid
                    if (span.IndexOf((byte)0) != -1)
                    {
                        throw new InvalidOperationException();
                    }
                    try
                    {
                        resultString = DefaultEncoding.GetString(buffer, span.Length);
                    }
                    catch (DecoderFallbackException)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            return resultString;
        }

        internal static byte[] GetBytes(string myString)
        {
            return DefaultEncoding.GetBytes(myString);
        }
    }
}

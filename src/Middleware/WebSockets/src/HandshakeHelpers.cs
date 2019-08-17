// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.WebSockets
{
    internal static class HandshakeHelpers
    {
        /// <summary>
        /// Gets request headers needed process the handshake on the server.
        /// </summary>
        public static readonly IEnumerable<string> NeededHeaders = new[]
        {
            HeaderNames.Upgrade,
            HeaderNames.Connection,
            HeaderNames.SecWebSocketKey,
            HeaderNames.SecWebSocketVersion
        };

        // Verify Method, Upgrade, Connection, version,  key, etc..
        public static bool CheckSupportedWebSocketRequest(string method, IEnumerable<KeyValuePair<string, string>> headers)
        {
            bool validUpgrade = false, validConnection = false, validKey = false, validVersion = false;

            if (!string.Equals("GET", method, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            foreach (var pair in headers)
            {
                if (string.Equals(HeaderNames.Connection, pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(Constants.Headers.ConnectionUpgrade, pair.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        validConnection = true;
                    }
                }
                else if (string.Equals(HeaderNames.Upgrade, pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(Constants.Headers.UpgradeWebSocket, pair.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        validUpgrade = true;
                    }
                }
                else if (string.Equals(HeaderNames.SecWebSocketVersion, pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(Constants.Headers.SupportedVersion, pair.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        validVersion = true;
                    }
                }
                else if (string.Equals(HeaderNames.SecWebSocketKey, pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    validKey = IsRequestKeyValid(pair.Value);
                }
            }

            return validConnection && validUpgrade && validVersion && validKey;
        }

        public static void GenerateResponseHeaders(string key, string subProtocol, IHeaderDictionary headers)
        {
            headers[HeaderNames.Connection] = Constants.Headers.ConnectionUpgrade;
            headers[HeaderNames.Upgrade] = Constants.Headers.UpgradeWebSocket;
            headers[HeaderNames.SecWebSocketAccept] = CreateResponseKey(key);
            if (!string.IsNullOrWhiteSpace(subProtocol))
            {
                headers[HeaderNames.SecWebSocketProtocol] = subProtocol;
            }
        }

        /// <summary>
        /// Validates the Sec-WebSocket-Key request header
        /// </summary>
        /// <param name="value">The request-key to validate</param>
        /// <returns><c>true</c> if the key is valid, <c>false</c> otherwise</returns>
        public static bool IsRequestKeyValid(string value)
        {
            var chars = value.AsSpan();

            // The base64 decoded key should be 16 bytes long. Thus the base64
            // encoded key must be 24 chars long. So we can short-circuit the
            // validation if the length don't match.
            if (chars.Length != 24)
            {
                return false;
            }

            if (Ssse3.IsSupported)
            {
                return IsRequestKeyValidSse(chars);
            }

            Span<byte> temp = stackalloc byte[16];
            var success = Convert.TryFromBase64Chars(chars, temp, out int written);
            return written == 16 && success;
        }

        public static string CreateResponseKey(string requestKey)
        {
            // "The value of this header field is constructed by concatenating /key/, defined above in step 4
            // in Section 4.2.2, with the string "258EAFA5- E914-47DA-95CA-C5AB0DC85B11", taking the SHA-1 hash of
            // this concatenated value to obtain a 20-byte value and base64-encoding"
            // https://tools.ietf.org/html/rfc6455#section-4.2.2

            if (requestKey == null)
            {
                throw new ArgumentNullException(nameof(requestKey));
            }

            using (var algorithm = SHA1.Create())
            {
                string merged = requestKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                byte[] mergedBytes = Encoding.UTF8.GetBytes(merged);
                byte[] hashedBytes = algorithm.ComputeHash(mergedBytes);
                return Convert.ToBase64String(hashedBytes);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsRequestKeyValidSse(ReadOnlySpan<char> chars)
        {
            Debug.Assert(chars.Length == 24);

            // Outline of the algorithm:
            // 0. Elements 0..21 consists of the base64-alphabet, elements 22..23 are the padding
            //    chars (=)
            // 1. We read two char-vectors, and pack them to a single byte-vector with saturation.
            //    This vector contains of elements 0..15
            // 2. As the validation is idempotent, we read in the same way a second byte-vector,
            //    containing of elements 6..21
            // 3. Perform the validation of the base64-alphabet. A description how the validation works is given in
            //    https://github.com/dotnet/corefx/blob/bfe2c58a4536db9a257940277c5d94bf9e26929a/src/System.Memory/src/System/Buffers/Text/Base64Decoder.cs#L455-L503
            // 4. Elements 22..23 are compared to ==

            ref var src = ref MemoryMarshal.GetReference(chars);

            var vec0 = src.ReadVector128();
            var vec1 = Unsafe.Add(ref src, 6).ReadVector128();

            var lutLo = Unsafe.ReadUnaligned<Vector128<sbyte>>(ref MemoryMarshal.GetReference(s_sseDecodeLutLo));
            var lutHi = Unsafe.ReadUnaligned<Vector128<sbyte>>(ref MemoryMarshal.GetReference(s_sseDecodeLutHi));
            var mask0F = Vector128.Create((sbyte)0x0F);

            var loNibbles0 = Sse2.And(vec0, mask0F);
            var loNibbles1 = Sse2.And(vec1, mask0F);
            var hiNibbles0 = Sse2.And(Sse2.ShiftRightLogical(vec0.AsInt32(), 4).AsSByte(), mask0F);
            var hiNibbles1 = Sse2.And(Sse2.ShiftRightLogical(vec1.AsInt32(), 4).AsSByte(), mask0F);

            var lo0 = Ssse3.Shuffle(lutLo, loNibbles0);
            var lo1 = Ssse3.Shuffle(lutLo, loNibbles1);
            var hi0 = Ssse3.Shuffle(lutHi, hiNibbles0);
            var hi1 = Ssse3.Shuffle(lutHi, hiNibbles1);

            var and0 = Sse2.And(lo0, hi0);
            var and1 = Sse2.And(lo1, hi1);
            var or = Sse2.Or(and0, and1);

            var gt = Sse2.CompareGreaterThan(or, Vector128<sbyte>.Zero);
            var mask = Sse2.MoveMask(gt);

            ref var lastTwoChars = ref Unsafe.Add(ref src, 22);
            var lastTwoCharsAsInt = Unsafe.ReadUnaligned<int>(ref Unsafe.As<char, byte>(ref lastTwoChars));
            const int twoPaddingCharsAsInt = '=' << 16 | '=';

            // PERF: JIT produces branchless code for the subtraction and comparison to 0
            // return mask == 0 && lastTwoChars == twoPaddingCharsAsInt;
            return ((lastTwoCharsAsInt - twoPaddingCharsAsInt) | mask) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<sbyte> ReadVector128(this ref char src)
        {
           ref var bytes = ref Unsafe.As<char, byte>(ref src);
           var c0 = Unsafe.ReadUnaligned<Vector128<short>>(ref bytes);
           var c1 = Unsafe.ReadUnaligned<Vector128<short>>(ref Unsafe.Add(ref bytes, 16));
           var tmp = Sse2.PackUnsignedSaturate(c0, c1);

            return tmp.AsSByte();
        }

        private static ReadOnlySpan<byte> s_sseDecodeLutLo => new byte[16]
        {
            0x15, 0x11, 0x11, 0x11,
            0x11, 0x11, 0x11, 0x11,
            0x11, 0x11, 0x13, 0x1A,
            0x1B, 0x1B, 0x1B, 0x1A
        };

        private static ReadOnlySpan<byte> s_sseDecodeLutHi => new byte[16]
        {
            0x10, 0x10, 0x01, 0x02,
            0x04, 0x08, 0x04, 0x08,
            0x10, 0x10, 0x10, 0x10,
            0x10, 0x10, 0x10, 0x10
        };
    }
}

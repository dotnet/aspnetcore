// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Session;

// A byte[] equality comparer based on the SipHash-2-4 algorithm. Key differences:
// (a) we output 32-bit hashes instead of 64-bit hashes, and
// (b) we don't care about endianness since hashes are used only in hash tables
//     and aren't returned to user code.
//
// Derived from the implementation in SignalR and modified to address byte[] instead of string. This derived version is not for cryptographic use, just hash codes.
// https://github.com/aspnet/SignalR-Server/blob/75f74169c81a51780f195d06b798302b2d76dbde/src/Microsoft.AspNetCore.SignalR.Server/Infrastructure/SipHashBasedStringEqualityComparer.cs
// Derivative work of https://github.com/tanglebones/ch-siphash.
internal static class SipHash
{
    internal static int GetHashCode(byte[] bytes)
    {
        // Assume SipHash-2-4 is a strong PRF, therefore truncation to 32 bits is acceptable.
        return (int)SipHash_2_4_UlongCast_ForcedInline(bytes);
    }

    private static ulong SipHash_2_4_UlongCast_ForcedInline(byte[] bytes)
    {
        unsafe
        {
            ulong v0 = 0x736f6d6570736575;
            ulong v1 = 0x646f72616e646f6d;
            ulong v2 = 0x6c7967656e657261;
            ulong v3 = 0x7465646279746573;

            uint inlen = (uint)bytes.Length;
            fixed (byte* finb = bytes)
            {
                var b = ((ulong)inlen) << 56;

                if (inlen > 0)
                {
                    var inb = finb;
                    var left = inlen & 7;
                    var end = inb + inlen - left;
                    var linb = (ulong*)finb;
                    var lend = (ulong*)end;
                    for (; linb < lend; ++linb)
                    {
                        v3 ^= *linb;

                        v0 += v1;
                        v1 = (v1 << 13) | (v1 >> (64 - 13));
                        v1 ^= v0;
                        v0 = (v0 << 32) | (v0 >> (64 - 32));

                        v2 += v3;
                        v3 = (v3 << 16) | (v3 >> (64 - 16));
                        v3 ^= v2;

                        v0 += v3;
                        v3 = (v3 << 21) | (v3 >> (64 - 21));
                        v3 ^= v0;

                        v2 += v1;
                        v1 = (v1 << 17) | (v1 >> (64 - 17));
                        v1 ^= v2;
                        v2 = (v2 << 32) | (v2 >> (64 - 32));
                        v0 += v1;
                        v1 = (v1 << 13) | (v1 >> (64 - 13));
                        v1 ^= v0;
                        v0 = (v0 << 32) | (v0 >> (64 - 32));

                        v2 += v3;
                        v3 = (v3 << 16) | (v3 >> (64 - 16));
                        v3 ^= v2;

                        v0 += v3;
                        v3 = (v3 << 21) | (v3 >> (64 - 21));
                        v3 ^= v0;

                        v2 += v1;
                        v1 = (v1 << 17) | (v1 >> (64 - 17));
                        v1 ^= v2;
                        v2 = (v2 << 32) | (v2 >> (64 - 32));

                        v0 ^= *linb;
                    }
                    for (var i = 0; i < left; ++i)
                    {
                        b |= ((ulong)end[i]) << (8 * i);
                    }
                }

                v3 ^= b;
                v0 += v1;
                v1 = (v1 << 13) | (v1 >> (64 - 13));
                v1 ^= v0;
                v0 = (v0 << 32) | (v0 >> (64 - 32));

                v2 += v3;
                v3 = (v3 << 16) | (v3 >> (64 - 16));
                v3 ^= v2;

                v0 += v3;
                v3 = (v3 << 21) | (v3 >> (64 - 21));
                v3 ^= v0;

                v2 += v1;
                v1 = (v1 << 17) | (v1 >> (64 - 17));
                v1 ^= v2;
                v2 = (v2 << 32) | (v2 >> (64 - 32));
                v0 += v1;
                v1 = (v1 << 13) | (v1 >> (64 - 13));
                v1 ^= v0;
                v0 = (v0 << 32) | (v0 >> (64 - 32));

                v2 += v3;
                v3 = (v3 << 16) | (v3 >> (64 - 16));
                v3 ^= v2;

                v0 += v3;
                v3 = (v3 << 21) | (v3 >> (64 - 21));
                v3 ^= v0;

                v2 += v1;
                v1 = (v1 << 17) | (v1 >> (64 - 17));
                v1 ^= v2;
                v2 = (v2 << 32) | (v2 >> (64 - 32));
                v0 ^= b;
                v2 ^= 0xff;

                v0 += v1;
                v1 = (v1 << 13) | (v1 >> (64 - 13));
                v1 ^= v0;
                v0 = (v0 << 32) | (v0 >> (64 - 32));

                v2 += v3;
                v3 = (v3 << 16) | (v3 >> (64 - 16));
                v3 ^= v2;

                v0 += v3;
                v3 = (v3 << 21) | (v3 >> (64 - 21));
                v3 ^= v0;

                v2 += v1;
                v1 = (v1 << 17) | (v1 >> (64 - 17));
                v1 ^= v2;
                v2 = (v2 << 32) | (v2 >> (64 - 32));
                v0 += v1;
                v1 = (v1 << 13) | (v1 >> (64 - 13));
                v1 ^= v0;
                v0 = (v0 << 32) | (v0 >> (64 - 32));

                v2 += v3;
                v3 = (v3 << 16) | (v3 >> (64 - 16));
                v3 ^= v2;

                v0 += v3;
                v3 = (v3 << 21) | (v3 >> (64 - 21));
                v3 ^= v0;

                v2 += v1;
                v1 = (v1 << 17) | (v1 >> (64 - 17));
                v1 ^= v2;
                v2 = (v2 << 32) | (v2 >> (64 - 32));
                v0 += v1;
                v1 = (v1 << 13) | (v1 >> (64 - 13));
                v1 ^= v0;
                v0 = (v0 << 32) | (v0 >> (64 - 32));

                v2 += v3;
                v3 = (v3 << 16) | (v3 >> (64 - 16));
                v3 ^= v2;

                v0 += v3;
                v3 = (v3 << 21) | (v3 >> (64 - 21));
                v3 ^= v0;

                v2 += v1;
                v1 = (v1 << 17) | (v1 >> (64 - 17));
                v1 ^= v2;
                v2 = (v2 << 32) | (v2 >> (64 - 32));
                v0 += v1;
                v1 = (v1 << 13) | (v1 >> (64 - 13));
                v1 ^= v0;
                v0 = (v0 << 32) | (v0 >> (64 - 32));

                v2 += v3;
                v3 = (v3 << 16) | (v3 >> (64 - 16));
                v3 ^= v2;

                v0 += v3;
                v3 = (v3 << 21) | (v3 >> (64 - 21));
                v3 ^= v0;

                v2 += v1;
                v1 = (v1 << 17) | (v1 >> (64 - 17));
                v1 ^= v2;
                v2 = (v2 << 32) | (v2 >> (64 - 32));
            }

            return v0 ^ v1 ^ v2 ^ v3;
        }
    }
}

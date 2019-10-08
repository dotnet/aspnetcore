// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.AspNetCore.Connections
{
    internal static class CorrelationIdGenerator
    {
        // Base32 encoding - in ascii sort order for easy text based sorting
        // s_encode32Bytes: 0123456789ABCDEFGHIJKLMNOPQRSTUV
        // use this optimization: https://github.com/dotnet/roslyn/pull/24621
        private static ReadOnlySpan<byte> s_encode32Bytes => new byte[] { (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'F', (byte)'G', (byte)'H', (byte)'I', (byte)'J', (byte)'K', (byte)'L', (byte)'M', (byte)'N', (byte)'O', (byte)'P', (byte)'Q', (byte)'R', (byte)'S', (byte)'T', (byte)'U', (byte)'V' };

        // Seed the _lastConnectionId for this application instance with
        // the number of 100-nanosecond intervals that have elapsed since 12:00:00 midnight, January 1, 0001
        // for a roughly increasing _lastId over restarts
        private static long _lastId = DateTime.UtcNow.Ticks;

        public static string GetNextId() => GenerateId(Interlocked.Increment(ref _lastId));

        private unsafe static string GenerateId(long id)
        {
            return string.Create(13, id, (buffer, value) =>
            {
                Span<byte> targetBytes = MemoryMarshal.Cast<char, byte>(buffer);

                // ReadOnlySpan<byte> doesn't support long index, have to use pointer to avoid cast long to int.
                fixed (byte* bp = s_encode32Bytes)
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        targetBytes[24] = bp[value & 31];
                        targetBytes[22] = bp[(value >> 5) & 31];
                        targetBytes[20] = bp[(value >> 10) & 31];
                        targetBytes[18] = bp[(value >> 15) & 31];
                        targetBytes[16] = bp[(value >> 20) & 31];
                        targetBytes[14] = bp[(value >> 25) & 31];
                        targetBytes[12] = bp[(value >> 30) & 31];
                        targetBytes[10] = bp[(value >> 35) & 31];
                        targetBytes[8] = bp[(value >> 40) & 31];
                        targetBytes[6] = bp[(value >> 45) & 31];
                        targetBytes[4] = bp[(value >> 50) & 31];
                        targetBytes[2] = bp[(value >> 55) & 31];
                        targetBytes[0] = bp[(value >> 60) & 31];
                    }
                    else
                    {
                        targetBytes[25] = bp[value & 31];
                        targetBytes[23] = bp[(value >> 5) & 31];
                        targetBytes[21] = bp[(value >> 10) & 31];
                        targetBytes[19] = bp[(value >> 15) & 31];
                        targetBytes[17] = bp[(value >> 20) & 31];
                        targetBytes[15] = bp[(value >> 25) & 31];
                        targetBytes[13] = bp[(value >> 30) & 31];
                        targetBytes[11] = bp[(value >> 35) & 31];
                        targetBytes[9] = bp[(value >> 40) & 31];
                        targetBytes[7] = bp[(value >> 45) & 31];
                        targetBytes[5] = bp[(value >> 50) & 31];
                        targetBytes[3] = bp[(value >> 55) & 31];
                        targetBytes[1] = bp[(value >> 60) & 31];
                    }
                }
            });
        }
    }
}

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
                Span<byte> bufferBytes = MemoryMarshal.Cast<char, byte>(buffer);

                // ReadOnlySpan<byte> doesn't support long index, have to use pointer to avoid cast long to int.
                fixed (byte* encode32Bytes = s_encode32Bytes)
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        bufferBytes[24] = encode32Bytes[value & 31];
                        bufferBytes[22] = encode32Bytes[(value >> 5) & 31];
                        bufferBytes[20] = encode32Bytes[(value >> 10) & 31];
                        bufferBytes[18] = encode32Bytes[(value >> 15) & 31];
                        bufferBytes[16] = encode32Bytes[(value >> 20) & 31];
                        bufferBytes[14] = encode32Bytes[(value >> 25) & 31];
                        bufferBytes[12] = encode32Bytes[(value >> 30) & 31];
                        bufferBytes[10] = encode32Bytes[(value >> 35) & 31];
                        bufferBytes[8] = encode32Bytes[(value >> 40) & 31];
                        bufferBytes[6] = encode32Bytes[(value >> 45) & 31];
                        bufferBytes[4] = encode32Bytes[(value >> 50) & 31];
                        bufferBytes[2] = encode32Bytes[(value >> 55) & 31];
                        bufferBytes[0] = encode32Bytes[(value >> 60) & 31];
                    }
                    else
                    {
                        bufferBytes[25] = encode32Bytes[value & 31];
                        bufferBytes[23] = encode32Bytes[(value >> 5) & 31];
                        bufferBytes[21] = encode32Bytes[(value >> 10) & 31];
                        bufferBytes[19] = encode32Bytes[(value >> 15) & 31];
                        bufferBytes[17] = encode32Bytes[(value >> 20) & 31];
                        bufferBytes[15] = encode32Bytes[(value >> 25) & 31];
                        bufferBytes[13] = encode32Bytes[(value >> 30) & 31];
                        bufferBytes[11] = encode32Bytes[(value >> 35) & 31];
                        bufferBytes[9] = encode32Bytes[(value >> 40) & 31];
                        bufferBytes[7] = encode32Bytes[(value >> 45) & 31];
                        bufferBytes[5] = encode32Bytes[(value >> 50) & 31];
                        bufferBytes[3] = encode32Bytes[(value >> 55) & 31];
                        bufferBytes[1] = encode32Bytes[(value >> 60) & 31];
                    }
                }
            });
        }
    }
}

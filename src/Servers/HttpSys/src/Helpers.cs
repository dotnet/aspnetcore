// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal static class Helpers
{
    public static PinnedSpan ChunkTerminator { get; } = new PinnedSpan("0\r\n\r\n"u8);
    public static PinnedSpan CRLF { get; } = new PinnedSpan("\r\n"u8);

    public readonly unsafe struct PinnedSpan
    {
        public PinnedSpan(ReadOnlySpan<byte> span)
        {
            // This copy can go away if UTF8 literals are guaranteed to be pinned
            // See https://github.com/dotnet/csharplang/issues/5295
            Ptr = (void*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(Helpers), span.Length);
            Length = span.Length;
            span.CopyTo(new Span<byte>(Ptr, Length));
        }
        public readonly void* Ptr;
        public readonly int Length;
    }

    internal static ArraySegment<byte> GetChunkHeader(long size)
    {
        if (size < int.MaxValue)
        {
            return GetChunkHeader((int)size);
        }

        // Greater than 2gb, perf is no longer our concern
        return new ArraySegment<byte>(Encoding.ASCII.GetBytes(size.ToString("X", CultureInfo.InvariantCulture) + "\r\n"));
    }

    /// <summary>
    /// A private utility routine to convert an integer to a chunk header,
    /// which is an ASCII hex number followed by a CRLF.The header is returned
    /// as a byte array.
    /// Generates a right-aligned hex string and returns the start offset.
    /// </summary>
    /// <param name="size">Chunk size to be encoded</param>
    /// <returns>A byte array with the header in int.</returns>
    internal static ArraySegment<byte> GetChunkHeader(int size)
    {
        uint mask = 0xf0000000;
        byte[] header = new byte[10];
        int i;
        int offset = -1;

        // Loop through the size, looking at each nibble. If it's not 0
        // convert it to hex. Save the index of the first non-zero
        // byte.

        for (i = 0; i < 8; i++, size <<= 4)
        {
            // offset == -1 means that we haven't found a non-zero nibble
            // yet. If we haven't found one, and the current one is zero,
            // don't do anything.

            if (offset == -1)
            {
                if ((size & mask) == 0)
                {
                    continue;
                }
            }

            // Either we have a non-zero nibble or we're no longer skipping
            // leading zeros. Convert this nibble to ASCII and save it.

            uint temp = (uint)size >> 28;

            if (temp < 10)
            {
                header[i] = (byte)(temp + '0');
            }
            else
            {
                header[i] = (byte)((temp - 10) + 'A');
            }

            // If we haven't found a non-zero nibble yet, we've found one
            // now, so remember that.

            if (offset == -1)
            {
                offset = i;
            }
        }

        header[8] = (byte)'\r';
        header[9] = (byte)'\n';

        return new ArraySegment<byte>(header, offset, header.Length - offset);
    }
}

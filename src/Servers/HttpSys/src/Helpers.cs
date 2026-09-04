// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal static class Helpers
{
    private static ReadOnlySpan<byte> ChunkTerminator => "0\r\n\r\n"u8;
    private static ReadOnlySpan<byte> CRLF => "\r\n"u8;

    // HTTP.SYS reads a chunk's buffer asynchronously, long after the send has been
    // queued, so the address has to stay valid for at least that long. These two are
    // constants, so keep a single copy of each on the pinned object heap: the address
    // is then stable for the life of the process and the chunks below can be built
    // once, leaving callers with nothing to pin and no lifetime to track.
    private static readonly byte[] PinnedChunkTerminator = AllocatePinned(ChunkTerminator);
    private static readonly byte[] PinnedCRLF = AllocatePinned(CRLF);

    internal static readonly HTTP_DATA_CHUNK ChunkTerminatorChunk = CreateMemoryChunk(PinnedChunkTerminator);
    internal static readonly HTTP_DATA_CHUNK CRLFChunk = CreateMemoryChunk(PinnedCRLF);

    private static byte[] AllocatePinned(ReadOnlySpan<byte> bytes)
    {
        var pinned = GC.AllocateUninitializedArray<byte>(bytes.Length, pinned: true);
        bytes.CopyTo(pinned);
        return pinned;
    }

    private static unsafe HTTP_DATA_CHUNK CreateMemoryChunk(byte[] pinnedBuffer)
    {
        var chunk = default(HTTP_DATA_CHUNK);
        chunk.DataChunkType = HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
        chunk.Anonymous.FromMemory.pBuffer = (void*)Marshal.UnsafeAddrOfPinnedArrayElement(pinnedBuffer, 0);
        chunk.Anonymous.FromMemory.BufferLength = (uint)pinnedBuffer.Length;
        return chunk;
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

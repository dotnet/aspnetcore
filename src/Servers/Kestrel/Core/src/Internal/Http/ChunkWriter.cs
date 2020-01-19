// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal static class ChunkWriter
    {
        public static int BeginChunkBytes(int dataCount, Span<byte> span)
        {
            // Determine the most-significant non-zero nibble
            int total, shift;
            var count = dataCount;
            total = (count > 0xffff) ? 0x10 : 0x00;
            count >>= total;
            shift = (count > 0x00ff) ? 0x08 : 0x00;
            count >>= shift;
            total |= shift;
            total |= (count > 0x000f) ? 0x04 : 0x00;

            count = (total >> 2) + 3;

            // This must be explicity typed as ReadOnlySpan<byte>
            // It then becomes a non-allocating mapping to the data section of the assembly.
            // For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
            ReadOnlySpan<byte> hex = new byte[16] { (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f' };

            var offset = 0;
            for (shift = total; shift >= 0; shift -= 4)
            {
                // Uses dotnet/runtime#1644 to elide the bounds check on hex as the & 0x0f definitely
                // constrains it to the range 0x0 - 0xf, matching the bounds of the array.
                span[offset] = hex[(dataCount >> shift) & 0x0f];
                offset++;
            }

            span[count - 2] = (byte)'\r';
            span[count - 1] = (byte)'\n';

            return count;
        }

        internal static int GetPrefixBytesForChunk(int length, out bool sliceOneByte)
        {
            sliceOneByte = false;
            // If GetMemory returns one of the following values, there is no way to set the prefix/body lengths
            // such that we either wouldn't have an invalid chunk or would need to copy if the entire memory chunk is used.
            // For example, if GetMemory returned 21, we would guess that the chunked prefix is 4 bytes initially
            // and the suffix is 2 bytes, meaning there is 15 bytes remaining to write into. However, 15 bytes only need 3
            // bytes for the chunked prefix, so we would have to copy once we call advance. Therefore, to avoid this scenario,
            // we slice the memory by one byte.

            // See https://gist.github.com/halter73/af2b9f78978f83813b19e187c4e5309e if you would like to tweak the algorithm at all.

            if (length <= 65544)
            {
                if (length <= 262)
                {
                    if (length <= 21)
                    {
                        if (length == 21)
                        {
                            sliceOneByte = true;
                        }
                        return 3;
                    }
                    else
                    {
                        if (length == 262)
                        {
                            sliceOneByte = true;
                        }
                        return 4;
                    }
                }
                else
                {
                    if (length <= 4103)
                    {
                        if (length == 4103)
                        {
                            sliceOneByte = true;
                        }
                        return 5;
                    }
                    else
                    {
                        if (length == 65544)
                        {
                            sliceOneByte = true;
                        }
                        return 6;
                    }
                }
            }
            else
            {
                if (length <= 16777226)
                {
                    if (length <= 1048585)
                    {
                        if (length == 1048585)
                        {
                            sliceOneByte = true;
                        }
                        return 7;
                    }
                    else
                    {
                        if (length == 16777226)
                        {
                            sliceOneByte = true;
                        }
                        return 8;
                    }
                }
                else
                {
                    if (length <= 268435467)
                    {
                        if (length == 268435467)
                        {
                            sliceOneByte = true;
                        }
                        return 9;
                    }
                    else
                    {
                        return 10;
                    }
                }
            }
        }

        internal static int WriteBeginChunkBytes(this ref BufferWriter<PipeWriter> start, int dataCount)
        {
            // 10 bytes is max length + \r\n
            start.Ensure(10);

            var count = BeginChunkBytes(dataCount, start.Span);
            start.Advance(count);
            return count;
        }

        internal static void WriteEndChunkBytes(this ref BufferWriter<PipeWriter> start)
        {
            start.Ensure(2);
            var span = start.Span;

            // CRLF done in reverse order so the 1st index will elide the bounds check for the 0th index
            span[1] = (byte)'\n';
            span[0] = (byte)'\r';
            start.Advance(2);
        }
    }
}

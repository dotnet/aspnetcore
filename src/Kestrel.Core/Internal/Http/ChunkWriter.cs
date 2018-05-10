// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal static class ChunkWriter
    {
        private static readonly ArraySegment<byte> _endChunkBytes = CreateAsciiByteArraySegment("\r\n");
        private static readonly byte[] _hex = Encoding.ASCII.GetBytes("0123456789abcdef");

        private static ArraySegment<byte> CreateAsciiByteArraySegment(string text)
        {
            var bytes = Encoding.ASCII.GetBytes(text);
            return new ArraySegment<byte>(bytes);
        }

        public static ArraySegment<byte> BeginChunkBytes(int dataCount)
        {
            var bytes = new byte[10]
            {
                _hex[((dataCount >> 0x1c) & 0x0f)],
                _hex[((dataCount >> 0x18) & 0x0f)],
                _hex[((dataCount >> 0x14) & 0x0f)],
                _hex[((dataCount >> 0x10) & 0x0f)],
                _hex[((dataCount >> 0x0c) & 0x0f)],
                _hex[((dataCount >> 0x08) & 0x0f)],
                _hex[((dataCount >> 0x04) & 0x0f)],
                _hex[((dataCount >> 0x00) & 0x0f)],
                (byte)'\r',
                (byte)'\n',
            };

            // Determine the most-significant non-zero nibble
            int total, shift;
            total = (dataCount > 0xffff) ? 0x10 : 0x00;
            dataCount >>= total;
            shift = (dataCount > 0x00ff) ? 0x08 : 0x00;
            dataCount >>= shift;
            total |= shift;
            total |= (dataCount > 0x000f) ? 0x04 : 0x00;

            var offset = 7 - (total >> 2);
            return new ArraySegment<byte>(bytes, offset, 10 - offset);
        }

        internal static int WriteBeginChunkBytes(ref CountingBufferWriter<PipeWriter> start, int dataCount)
        {
            var chunkSegment = BeginChunkBytes(dataCount);
            start.Write(new ReadOnlySpan<byte>(chunkSegment.Array, chunkSegment.Offset, chunkSegment.Count));
            return chunkSegment.Count;
        }

        internal static void WriteEndChunkBytes(ref CountingBufferWriter<PipeWriter> start)
        {
            start.Write(new ReadOnlySpan<byte>(_endChunkBytes.Array, _endChunkBytes.Offset, _endChunkBytes.Count));
        }
    }
}

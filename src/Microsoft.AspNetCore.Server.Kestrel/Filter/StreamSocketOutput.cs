// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Http;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Filter
{
    public class StreamSocketOutput : ISocketOutput
    {
        private static readonly byte[] _endChunkBytes = Encoding.ASCII.GetBytes("\r\n");
        private static readonly byte[] _nullBuffer = new byte[0];

        private readonly Stream _outputStream;
        private readonly MemoryPool2 _memory;
        private MemoryPoolBlock2 _producingBlock;

        private object _writeLock = new object();

        public StreamSocketOutput(Stream outputStream, MemoryPool2 memory)
        {
            _outputStream = outputStream;
            _memory = memory;
        }

        public void Write(ArraySegment<byte> buffer, bool immediate, bool chunk)
        {
            lock (_writeLock)
            {
                if (chunk && buffer.Array != null)
                {
                    var beginChunkBytes = ChunkWriter.BeginChunkBytes(buffer.Count);
                    _outputStream.Write(beginChunkBytes.Array, beginChunkBytes.Offset, beginChunkBytes.Count);
                }

                _outputStream.Write(buffer.Array ?? _nullBuffer, buffer.Offset, buffer.Count);

                if (chunk && buffer.Array != null)
                {
                    _outputStream.Write(_endChunkBytes, 0, _endChunkBytes.Length);
                }
            }
        }

        public Task WriteAsync(ArraySegment<byte> buffer, bool immediate, bool chunk, CancellationToken cancellationToken)
        {
            // TODO: Use _outputStream.WriteAsync
            Write(buffer, immediate, chunk);
            return TaskUtilities.CompletedTask;
        }

        public MemoryPoolIterator2 ProducingStart()
        {
            _producingBlock = _memory.Lease();
            return new MemoryPoolIterator2(_producingBlock);
        }

        public void ProducingComplete(MemoryPoolIterator2 end)
        {
            var block = _producingBlock;
            while (block != end.Block)
            {
                _outputStream.Write(block.Data.Array, block.Data.Offset, block.Data.Count);

                var returnBlock = block;
                block = block.Next;
                returnBlock.Pool.Return(returnBlock);
            }

            _outputStream.Write(end.Block.Array, end.Block.Data.Offset, end.Index - end.Block.Data.Offset);
            end.Block.Pool.Return(end.Block);
        }
    }
}

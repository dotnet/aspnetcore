// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNet.Server.Kestrel.Filter
{
    public class StreamSocketOutput : ISocketOutput
    {
        private readonly Stream _outputStream;
        private readonly MemoryPool2 _memory;
        private MemoryPoolBlock2 _producingBlock;

        public StreamSocketOutput(Stream outputStream, MemoryPool2 memory)
        {
            _outputStream = outputStream;
            _memory = memory;
        }

        void ISocketOutput.Write(ArraySegment<byte> buffer, bool immediate)
        {
            _outputStream.Write(buffer.Array, buffer.Offset, buffer.Count);
        }

        Task ISocketOutput.WriteAsync(ArraySegment<byte> buffer, bool immediate, CancellationToken cancellationToken)
        {
            // TODO: Use _outputStream.WriteAsync
            _outputStream.Write(buffer.Array, buffer.Offset, buffer.Count);
            return TaskUtilities.CompletedTask;
        }

        public MemoryPoolIterator2 ProducingStart()
        {
            _producingBlock = _memory.Lease();
            return new MemoryPoolIterator2(_producingBlock);
        }

        public void ProducingComplete(MemoryPoolIterator2 end, int count)
        {
            var block = _producingBlock;
            while (block != end.Block)
            {
                _outputStream.Write(block.Data.Array, block.Data.Offset, block.Data.Count);

                var returnBlock = block;
                block = block.Next;
                returnBlock.Pool?.Return(returnBlock);
            }

            _outputStream.Write(end.Block.Array, end.Block.Data.Offset, end.Index - end.Block.Data.Offset);
            end.Block.Pool?.Return(end.Block);
        }
    }
}

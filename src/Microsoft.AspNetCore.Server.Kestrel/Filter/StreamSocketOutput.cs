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

        private readonly string _connectionId;
        private readonly Stream _outputStream;
        private readonly MemoryPool _memory;
        private readonly IKestrelTrace _logger;
        private MemoryPoolBlock _producingBlock;

        private bool _canWrite = true;

        private object _writeLock = new object();

        public StreamSocketOutput(string connectionId, Stream outputStream, MemoryPool memory, IKestrelTrace logger)
        {
            _connectionId = connectionId;
            _outputStream = outputStream;
            _memory = memory;
            _logger = logger;
        }

        public void Write(ArraySegment<byte> buffer, bool chunk)
        {
            lock (_writeLock)
            {
                if (buffer.Count == 0 )
                {
                    return;
                }

                try
                {
                    if (!_canWrite)
                    {
                        return;
                    }

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
                catch (Exception ex)
                {
                    _canWrite = false;
                    _logger.ConnectionError(_connectionId, ex);
                }
            }
        }

        public Task WriteAsync(ArraySegment<byte> buffer, bool chunk, CancellationToken cancellationToken)
        {
#if NET451
            Write(buffer, chunk);
            return TaskUtilities.CompletedTask;
#else
            if (chunk && buffer.Array != null)
            {
                return WriteAsyncChunked(buffer, cancellationToken);
            }

            return _outputStream.WriteAsync(buffer.Array ?? _nullBuffer, buffer.Offset, buffer.Count, cancellationToken);
#endif
        }

        private async Task WriteAsyncChunked(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            var beginChunkBytes = ChunkWriter.BeginChunkBytes(buffer.Count);

            await _outputStream.WriteAsync(beginChunkBytes.Array, beginChunkBytes.Offset, beginChunkBytes.Count, cancellationToken);
            await _outputStream.WriteAsync(buffer.Array, buffer.Offset, buffer.Count, cancellationToken);
            await _outputStream.WriteAsync(_endChunkBytes, 0, _endChunkBytes.Length, cancellationToken);
        }

        public MemoryPoolIterator ProducingStart()
        {
            _producingBlock = _memory.Lease();
            return new MemoryPoolIterator(_producingBlock);
        }

        public void ProducingComplete(MemoryPoolIterator end)
        {
            var block = _producingBlock;
            while (block != end.Block)
            {
                // If we don't handle an exception from _outputStream.Write() here, we'll leak memory blocks.
                if (_canWrite)
                {
                    try
                    {
                         _outputStream.Write(block.Data.Array, block.Data.Offset, block.Data.Count);
                    }
                    catch (Exception ex)
                    {
                        _canWrite = false;
                        _logger.ConnectionError(_connectionId, ex);
                    }
                }

                var returnBlock = block;
                block = block.Next;
                returnBlock.Pool.Return(returnBlock);
            }
            
            if (_canWrite)
            {
                try
                {
                    _outputStream.Write(end.Block.Array, end.Block.Data.Offset, end.Index - end.Block.Data.Offset);
                }
                catch (Exception ex)
                {
                    _canWrite = false;
                    _logger.ConnectionError(_connectionId, ex);
                }
            }

            end.Block.Pool.Return(end.Block);
        }
    }
}

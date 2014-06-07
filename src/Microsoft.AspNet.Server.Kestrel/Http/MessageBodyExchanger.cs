// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    /// <summary>
    /// Summary description for MessageBodyExchanger
    /// </summary>
    public class MessageBodyExchanger
    {
        private static readonly WaitCallback _completePending = CompletePending;
        protected readonly ConnectionContext _context;

        object _sync = new Object();

        ArraySegment<byte> _buffer;
        Queue<ReadOperation> _reads = new Queue<ReadOperation>();

        public MessageBodyExchanger(ConnectionContext context)
        {
            _context = context;
            _buffer = new ArraySegment<byte>(_context.Memory.Empty);
        }

        public bool LocalIntakeFin { get; set; }

        public void Transfer(int count, bool fin)
        {
            var input = _context.SocketInput;
            lock (_sync)
            {
                // NOTE: this should not copy each time
                var oldBuffer = _buffer;
                var newData = _context.SocketInput.Take(count);

                var newBuffer = new ArraySegment<byte>(
                    _context.Memory.AllocByte(oldBuffer.Count + newData.Count),
                    0,
                    oldBuffer.Count + newData.Count);

                Array.Copy(oldBuffer.Array, oldBuffer.Offset, newBuffer.Array, newBuffer.Offset, oldBuffer.Count);
                Array.Copy(newData.Array, newData.Offset, newBuffer.Array, newBuffer.Offset + oldBuffer.Count, newData.Count);

                _buffer = newBuffer;
                _context.Memory.FreeByte(oldBuffer.Array);

                if (fin)
                {
                    LocalIntakeFin = true;
                }
                if (_reads.Any())
                {
                    ThreadPool.QueueUserWorkItem(_completePending, this);
                }
            }
        }

        public Task<int> ReadAsync(ArraySegment<byte> buffer)
        {
            for (; ;)
            {
                while (CompletePending())
                {
                    // earlier reads have priority
                }
                lock (_sync)
                {
                    if (_buffer.Count != 0 || buffer.Count == 0 || LocalIntakeFin)
                    {
                        // there is data we can take right now
                        if (_reads.Any())
                        {
                            // someone snuck in, try again
                            continue;
                        }

                        var count = Math.Min(buffer.Count, _buffer.Count);
                        Array.Copy(_buffer.Array, _buffer.Offset, buffer.Array, buffer.Offset, count);
                        _buffer = new ArraySegment<byte>(_buffer.Array, _buffer.Offset + count, _buffer.Count - count);
                        return Task.FromResult(count);
                    }
                    else
                    {
                        // add ourselves to the line
                        var tcs = new TaskCompletionSource<int>();
                        _reads.Enqueue(new ReadOperation
                        {
                            Buffer = buffer,
                            CompletionSource = tcs,
                        });
                        return tcs.Task;
                    }
                }
            }
        }

        static void CompletePending(object state)
        {
            while (((MessageBodyExchanger)state).CompletePending())
            {
                // loop until none left
            }
        }

        bool CompletePending()
        {
            ReadOperation read;
            int count;
            lock (_sync)
            {
                if (_buffer.Count == 0 && !LocalIntakeFin)
                {
                    return false;
                }
                if (!_reads.Any())
                {
                    return false;
                }
                read = _reads.Dequeue();

                count = Math.Min(read.Buffer.Count, _buffer.Count);
                Array.Copy(_buffer.Array, _buffer.Offset, read.Buffer.Array, read.Buffer.Offset, count);
                _buffer = new ArraySegment<byte>(_buffer.Array, _buffer.Offset + count, _buffer.Count - count);
            }
            if (read.CompletionSource != null)
            {
                read.CompletionSource.SetResult(count);
            }
            return true;
        }

        public struct ReadOperation
        {
            public TaskCompletionSource<int> CompletionSource;
            public ArraySegment<byte> Buffer;
        }
    }
}

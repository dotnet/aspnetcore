// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    internal sealed class SocketSender : SocketSenderReceiverBase
    {
        private List<ArraySegment<byte>> _bufferList;

        public SocketSender(Socket socket, PipeScheduler scheduler) : base(socket, scheduler)
        {
        }

        public SocketAwaitableEventArgs SendAsync(in ReadOnlySequence<byte> buffers)
        {
            if (buffers.IsSingleSegment)
            {
                return SendAsync(buffers.First);
            }

            if (!_awaitableEventArgs.MemoryBuffer.Equals(Memory<byte>.Empty))
            {
                _awaitableEventArgs.SetBuffer(null, 0, 0);
            }

            _awaitableEventArgs.BufferList = GetBufferList(buffers);

            if (!_socket.SendAsync(_awaitableEventArgs))
            {
                _awaitableEventArgs.Complete();
            }

            return _awaitableEventArgs;
        }

        private SocketAwaitableEventArgs SendAsync(ReadOnlyMemory<byte> memory)
        {
            // The BufferList getter is much less expensive then the setter.
            if (_awaitableEventArgs.BufferList != null)
            {
                _awaitableEventArgs.BufferList = null;
            }

            _awaitableEventArgs.SetBuffer(MemoryMarshal.AsMemory(memory));

            if (!_socket.SendAsync(_awaitableEventArgs))
            {
                _awaitableEventArgs.Complete();
            }

            return _awaitableEventArgs;
        }

        private List<ArraySegment<byte>> GetBufferList(in ReadOnlySequence<byte> buffer)
        {
            Debug.Assert(!buffer.IsEmpty);
            Debug.Assert(!buffer.IsSingleSegment);

            if (_bufferList == null)
            {
                _bufferList = new List<ArraySegment<byte>>();
            }
            else
            {
                // Buffers are pooled, so it's OK to root them until the next multi-buffer write.
                _bufferList.Clear();
            }

            foreach (var b in buffer)
            {
                _bufferList.Add(b.GetArray());
            }

            return _bufferList;
        }
    }
}

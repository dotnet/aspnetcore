// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;

internal sealed class SocketSender : SocketAwaitableEventArgs
{
    private List<ArraySegment<byte>>? _bufferList;

    public SocketSender(PipeScheduler scheduler) : base(scheduler)
    {
    }

    public ValueTask<SocketOperationResult> SendAsync(Socket socket, in ReadOnlySequence<byte> buffers)
    {
        if (buffers.IsSingleSegment)
        {
            return SendAsync(socket, buffers.First);
        }

        SetBufferList(buffers);

        if (socket.SendAsync(this))
        {
            return new ValueTask<SocketOperationResult>(this, 0);
        }

        var bytesTransferred = BytesTransferred;
        var error = SocketError;

        return error == SocketError.Success
            ? new ValueTask<SocketOperationResult>(new SocketOperationResult(bytesTransferred))
            : new ValueTask<SocketOperationResult>(new SocketOperationResult(CreateException(error)));
    }

    public void Reset()
    {
        // We clear the buffer and buffer list before we put it back into the pool
        // it's a small performance hit but it removes the confusion when looking at dumps to see this still
        // holds onto the buffer when it's back in the pool
        if (BufferList != null)
        {
            BufferList = null;

            _bufferList?.Clear();
        }
        else
        {
            SetBuffer(null, 0, 0);
        }
    }

    private ValueTask<SocketOperationResult> SendAsync(Socket socket, ReadOnlyMemory<byte> memory)
    {
        SetBuffer(MemoryMarshal.AsMemory(memory));

        if (socket.SendAsync(this))
        {
            return new ValueTask<SocketOperationResult>(this, 0);
        }

        var bytesTransferred = BytesTransferred;
        var error = SocketError;

        return error == SocketError.Success
            ? new ValueTask<SocketOperationResult>(new SocketOperationResult(bytesTransferred))
            : new ValueTask<SocketOperationResult>(new SocketOperationResult(CreateException(error)));
    }

    private void SetBufferList(in ReadOnlySequence<byte> buffer)
    {
        Debug.Assert(!buffer.IsEmpty);
        Debug.Assert(!buffer.IsSingleSegment);

        if (_bufferList == null)
        {
            _bufferList = new List<ArraySegment<byte>>();
        }

        foreach (var b in buffer)
        {
            _bufferList.Add(b.GetArray());
        }

        // The act of setting this list, sets the buffers in the internal buffer list
        BufferList = _bufferList;
    }
}

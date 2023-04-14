// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.AspNetCore.Http.Connections;

// Read from "network" 
// Parse framing and slice the read so the application doesn't see the framing
// Notify outbound pipe of framing details for when sending back
// Notify application pipe of ack id provided by other side of the network
internal sealed class ParseAckPipeReader : PipeReader
{
    private const int FrameSize = 24;
    private readonly PipeReader _inner;
    private readonly AckPipeWriter _ackPipeWriter;
    private readonly AckPipeReader _ackPipeReader;
    private long _totalBytes;
    private long _remaining;

    private ReadOnlySequence<byte> _currentRead;

    public ParseAckPipeReader(PipeReader inner, AckPipeWriter ackPipeWriter, AckPipeReader ackPipeReader)
    {
        _inner = inner;
        _ackPipeWriter = ackPipeWriter;
        _ackPipeReader = ackPipeReader;
    }

    public override void AdvanceTo(SequencePosition consumed)
    {
        CommonAdvance(ref consumed);
        _inner.AdvanceTo(consumed);
    }

    public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
    {
        CommonAdvance(ref consumed);
        _inner.AdvanceTo(consumed, examined);
    }

    private void CommonAdvance(ref SequencePosition consumed)
    {
        // Get the number of bytes consumed to update our internal state
        var len = _currentRead.Length;
        // This is used by ReadAsync to help update the ack id
        _currentRead = _currentRead.Slice(consumed);
        len -= _currentRead.Length;

        _remaining -= len;
    }

    public override void CancelPendingRead()
    {
        _inner.CancelPendingRead();
    }

    public override void Complete(Exception? exception = null)
    {
        _inner.Complete(exception);
    }

    public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        var res = await _inner.ReadAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var newBytes = res.Buffer.Length - _currentRead.Length;
            _currentRead = res.Buffer;

            if (res.IsCompleted || res.IsCanceled)
            {
                // TODO: figure out behavior
                if (res.Buffer.Length >= FrameSize)
                {
                    res = new(res.Buffer.Slice(FrameSize), res.IsCanceled, res.IsCompleted);
                }
                return res;
            }

            ReadOnlySequence<byte> buffer = res.Buffer;
            if (_remaining == 0)
            {
                // TODO: didn't get 24 bytes
                var frame = buffer.Slice(0, FrameSize);
                var len = ParseFrame(frame, _ackPipeReader);
                _totalBytes += len;

                _remaining = len;

                // if the buffer doesn't have enough data we need to update how much we're slicing
                if (len > buffer.Length - FrameSize)
                {
                    len = buffer.Length - FrameSize;
                }

                buffer = buffer.Slice(FrameSize, len);
                _currentRead = buffer;
                // 0 length means it was part of the reconnect handshake and not sent over the pipe, ignore it for acking purposes
                // TODO: check if 0 byte writes are possible in ConnectionHandlers and possibly handle them differently
                _ackPipeWriter.LastAck += buffer.Length == 0 ? 0 : buffer.Length + FrameSize;
            }
            else
            {
                // Advance was called and didn't consume everything even though we gave it the entire Frame Length of data
                // This means the caller is expecting more than a single frame of data
                // We'll need to start buffering to parse multiple frames of data
                if (_remaining <= _currentRead.Length && buffer.Length > _remaining)
                {
                    // TODO: multi-frame support
                }
                _ackPipeWriter.LastAck += Math.Min(_remaining, newBytes);
                _currentRead = buffer;
                buffer = buffer.Slice(0, Math.Min(_remaining, buffer.Length));
            }

            // TODO: validation everywhere!
            //Debug.Assert(len < res.Buffer.Length);

            res = new(buffer, res.IsCanceled, res.IsCompleted);

            // TODO: probably should avoid returning when we have 0 bytes to return (unless canceled/completed)
            //Debug.Assert(buffer.Length > 0);
        }
        catch (Exception ex)
        {
            _inner.Complete(ex);
            throw;
        }

        return res;
    }

    public static long ParseFrame(ReadOnlySequence<byte> frame, AckPipeReader ackPipeReader)
    {
        Debug.Assert(frame.Length >= FrameSize);
        frame = frame.Slice(0, FrameSize);

        long len;
        long ackId;

        // TODO: check perf of single Span check vs Stackalloc
        Span<byte> buffer = stackalloc byte[FrameSize];
        frame.CopyTo(buffer);
        var status = Base64.DecodeFromUtf8InPlace(buffer.Slice(0, FrameSize / 2), out var written);
        Debug.Assert(status == OperationStatus.Done);
        Debug.Assert(written == 8);

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        len = BitConverter.ToInt64(buffer);
#else
        var longBuf = new byte[8];
        buffer.Slice(0, 8).CopyTo(longBuf);
        len = BitConverter.ToInt64(longBuf, 0);
#endif

        status = Base64.DecodeFromUtf8InPlace(buffer.Slice(FrameSize / 2), out written);
        Debug.Assert(status == OperationStatus.Done);
        Debug.Assert(written == 8);

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        ackId = BitConverter.ToInt64(buffer.Slice(FrameSize / 2));
#else
        buffer.Slice(12, 8).CopyTo(longBuf);
        ackId = BitConverter.ToInt64(longBuf, 0);
#endif

        // Update ack id provided by other side, so the underlying pipe can release buffered memory
        ackPipeReader.Ack(ackId);
        return len;
    }

    public override bool TryRead(out ReadResult result)
    {
        throw new NotImplementedException();
    }
}

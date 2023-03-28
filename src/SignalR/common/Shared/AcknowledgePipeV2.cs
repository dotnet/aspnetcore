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

namespace PipelinesOverNetwork;

// Wrapper around a PipeReader that adds an Ack position which replaces Consumed
// This allows the underlying pipe to keep un-acked data in the pipe while still providing only new data to the reader
internal sealed class AckPipeReader : PipeReader
{
    private readonly PipeReader _inner;
    private readonly object _lock = new object();

    private SequencePosition _consumed;
    private SequencePosition _ackPosition;
    private long _ackDiff;
    private long _ackId;
    private long _totalWritten;
    private bool _resend;

    public AckPipeReader(PipeReader inner)
    {
        _inner = inner;
    }

    // Update the ack position. This number includes the framing size.
    // If byteID is larger than the total bytes sent, it'll throw InvalidOperationException.
    public void Ack(long byteID)
    {
        lock (_lock)
        {
            //Debug.Assert(_ackDiff == 0);
            // ignore? Is this a bad state?
            if (byteID < _ackId)
            {
                return;
            }
            //Debug.Assert(byteID >= _ackId);
            _ackDiff = byteID - _ackId;
            //Console.WriteLine($"AckId: {byteID}");

            if (_totalWritten < byteID)
            {
                Throw();
                static void Throw()
                {
                    throw new InvalidOperationException("Ack ID is greater than total amount of bytes that have been sent.");
                }
            }
        }
    }

    public void Resend()
    {
        Debug.Assert(_resend == false);
        if (_totalWritten == 0)
        {
            return;
        }
        _resend = true;
    }

    public override void AdvanceTo(SequencePosition consumed)
    {
        AdvanceTo(consumed, consumed);
    }

    public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
    {
        _consumed = consumed;
        if (_consumed.Equals(_ackPosition))
        {
            // Reset to default, we check this in ReadAsync to know if we should provide the current read buffer to the user
            // Or slice to the consumed position
            _consumed = default;
        }
        _inner.AdvanceTo(_ackPosition, examined);
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
        var buffer = res.Buffer;
        lock (_lock)
        {
            if (_ackDiff != 0)
            {
                // This detects the odd scenario where _consumed points to the end of a Segment and buffer.Slice(_ackDiff) points to the beginning of the next Segment
                // While they technically point to different positions, they point to the same concept of "beginning of the next buffer"
                var ackSlice = buffer.Slice(_ackDiff);
                if (buffer.Slice(_consumed).First.Length == 0 && ackSlice.Start.GetInteger() == 0)
                {
                    // Fix consumed to point to the beginning of the next Segment
                    _consumed = ackSlice.Start;
                    // wtf does this do though
                    //_consumed = buffer.Slice(buffer.Length - buffer.Slice(_consumed).Length).Start;
                }

                buffer = ackSlice;
                _ackId += _ackDiff;
                _ackDiff = 0;
                _ackPosition = buffer.Start;
            }
        }

        // Slice consumed, unless resending, then slice to ackPosition
        if (_resend)
        {
            _resend = false;
            buffer = buffer.Slice(_ackPosition);
            // update total written?
        }
        else
        {
            _ackPosition = buffer.Start;
            // TODO: buffer.Length is 0 sometimes, figure out why and verify behavior
            if (buffer.Length > 0 && !_consumed.Equals(default))
            {
                buffer = buffer.Slice(_consumed);
            }
            _totalWritten += (uint)buffer.Length;
        }
        res = new(buffer, res.IsCanceled, res.IsCompleted);
        return res;
    }

    public override bool TryRead(out ReadResult result)
    {
        throw new NotImplementedException();
    }
}

// Wrapper around a PipeWriter that adds framing to writes
internal sealed class AckPipeWriter : PipeWriter
{
    private const int FrameSize = 24;
    private readonly PipeWriter _inner;
    internal long lastAck;

    Memory<byte> _frameHeader;
    bool _shouldAdvanceFrameHeader;
    private long _buffered;

    public AckPipeWriter(PipeWriter inner)
    {
        _inner = inner;
    }

    public override void Advance(int bytes)
    {
        _buffered += bytes;
        if (_shouldAdvanceFrameHeader)
        {
            bytes += FrameSize;
            _shouldAdvanceFrameHeader = false;
        }
        _inner.Advance(bytes);
    }

    public override void CancelPendingFlush()
    {
        _inner.CancelPendingFlush();
    }

    public override void Complete(Exception? exception = null)
    {
        _inner.Complete(exception);
    }

    // X - 8 byte size of payload as uint
    // Y - 8 byte number of acked bytes
    // Z - payload
    // [ XXXX YYYY ZZZZ ]
    public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
    {
        Debug.Assert(_frameHeader.Length >= FrameSize);

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        var res = BitConverter.TryWriteBytes(_frameHeader.Span, _buffered);
        Debug.Assert(res);
        var status = Base64.EncodeToUtf8InPlace(_frameHeader.Span, 8, out var written);
        Debug.Assert(status == OperationStatus.Done);
        Debug.Assert(written == 12);
        res = BitConverter.TryWriteBytes(_frameHeader.Slice(12).Span, lastAck);
        Debug.Assert(res);
        status = Base64.EncodeToUtf8InPlace(_frameHeader.Slice(12).Span, 8, out written);
        Debug.Assert(status == OperationStatus.Done);
        Debug.Assert(written == 12);
#else
        BitConverter.GetBytes(_buffered).CopyTo(_frameHeader);
        var status = Base64.EncodeToUtf8InPlace(_frameHeader.Span, 8, out var written);
        Debug.Assert(status == OperationStatus.Done);
        Debug.Assert(written == 12);
        BitConverter.GetBytes(lastAck).CopyTo(_frameHeader.Slice(12).Span);
        status = Base64.EncodeToUtf8InPlace(_frameHeader.Slice(12).Span, 8, out written);
        Debug.Assert(status == OperationStatus.Done);
        Debug.Assert(written == 12);
#endif

        _frameHeader = Memory<byte>.Empty;
        _buffered = 0;
        return _inner.FlushAsync(cancellationToken);
    }

    public override Memory<byte> GetMemory(int sizeHint = 0)
    {
        var segment = _inner.GetMemory(Math.Max(FrameSize + 1, sizeHint));
        if (_frameHeader.IsEmpty || _buffered == 0)
        {
            Debug.Assert(segment.Length > FrameSize);

            _frameHeader = segment.Slice(0, FrameSize);
            segment = segment.Slice(FrameSize);
            _shouldAdvanceFrameHeader = true;
        }
        return segment;
    }

    public override Span<byte> GetSpan(int sizeHint = 0)
    {
        return GetMemory(sizeHint).Span;
    }
}

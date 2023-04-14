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
            _ackDiff = byteID - _ackId;

            if (_totalWritten < byteID)
            {
                Throw(byteID, _totalWritten);
                static void Throw(long id, long total)
                {
                    throw new InvalidOperationException($"Ack ID '{id}' is greater than total amount of '{total}' bytes that have been sent.");
                }
            }
        }
    }

    public bool Resend()
    {
        // TODO: Do we need to check this?
        Debug.Assert(_resend == false);
        if (_totalWritten == 0)
        {
            return false;
        }
        // Unblocks ReadAsync and gives a buffer with the examined but not consumed bytes
        // This avoids the issue where we have to wait for someone to write to the pipe before
        // the receive loop will see what might have been written during disconnect
        CancelPendingRead();
        _resend = true;
        return true;
    }

    public override void AdvanceTo(SequencePosition consumed)
    {
        AdvanceTo(consumed, consumed);
    }

    public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
    {
        _consumed = consumed;
        //if (_ackPosition.Equals(default))
        //{
        //    Debug.Assert(false);
        //    _inner.AdvanceTo(consumed, examined);
        //}
        //else
        //{
            _inner.AdvanceTo(_ackPosition, examined);
        //}

        if (_consumed.Equals(_ackPosition))
        {
            // Reset to default, we check this in ReadAsync to know if we should provide the current read buffer to the user
            // Or slice to the consumed position
            _consumed = default;
            _ackPosition = default;
        }
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
                }
                else if (!_consumed.Equals(default))
                {
                    var consumedLength = buffer.Slice(_consumed).Length;
                    if (consumedLength == ackSlice.Length)
                    {
                        _consumed = default;
                    }
                    else if (consumedLength > ackSlice.Length)
                    {
                        // ack is greater than consumed, should not be possible

                        // TODO: verify that if ack is less than total but more than consumed this isn't hit
                        // e.g. 13 bytes in underlying pipe, only consumed 11 during Read+Advance. Will an ack id of 12 be allowed?
                        Debug.Assert(false);
                    }
                    else if (consumedLength < ackSlice.Length)
                    {
                        // this is normal, ack id is less than total written
                    }
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
            if (buffer.Length != 0 && !_ackPosition.Equals(default))
            {
                buffer = buffer.Slice(_ackPosition);
            }
            // update total written if there is more written to the pipe during a reconnect
            // TODO: add tests for both these paths
            if (!_consumed.Equals(default))
            {
                Debug.Assert(buffer.Length - buffer.Slice(_consumed).Length >= 0);
                _totalWritten += buffer.Length - buffer.Slice(_consumed).Length;
            }
            else
            {
                _totalWritten += buffer.Length;
            }
        }
        else if (buffer.Length > 0)
        {
            _ackPosition = buffer.Start;
            if (!_consumed.Equals(default))
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
    public const int FrameSize = 24;
    private readonly PipeWriter _inner;
    internal long LastAck;

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

    // TODO: We could reduce this to 16 bytes for binary transports and avoid the base64 encode/decode
    // TODO: We could also reduce this to 1 + 12 (or 8) bytes occasionally if we add a flag for no new ack ID and avoid sending an ack
    // X - 12 byte - size of payload as long and base64 encoded
    // Y - 12 byte - number of acked bytes as long and base64 encoded
    // Z - payload
    // [ XXXX YYYY ZZZZ ]
    public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
    {
        Debug.Assert(_frameHeader.Length >= FrameSize);

        WriteFrame(_frameHeader.Span, _buffered, LastAck);

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

    public static void WriteFrame(Span<byte> header, long length, long ack)
    {
        Debug.Assert(header.Length >= FrameSize);

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        var res = BitConverter.TryWriteBytes(header, length);
        Debug.Assert(res);
        var status = Base64.EncodeToUtf8InPlace(header, 8, out var written);
        Debug.Assert(status == OperationStatus.Done);
        Debug.Assert(written == 12);
        res = BitConverter.TryWriteBytes(header.Slice(12), ack);
        Debug.Assert(res);
        status = Base64.EncodeToUtf8InPlace(header.Slice(12), 8, out written);
        Debug.Assert(status == OperationStatus.Done);
        Debug.Assert(written == 12);
#else
        BitConverter.GetBytes(length).CopyTo(header);
        var status = Base64.EncodeToUtf8InPlace(header, 8, out var written);
        Debug.Assert(status == OperationStatus.Done);
        Debug.Assert(written == 12);
        BitConverter.GetBytes(ack).CopyTo(header.Slice(12));
        status = Base64.EncodeToUtf8InPlace(header.Slice(12), 8, out written);
        Debug.Assert(status == OperationStatus.Done);
        Debug.Assert(written == 12);
#endif
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading.Tasks;
using System.Threading;
using System;

#nullable enable

namespace Microsoft.AspNetCore.Http.Connections;

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

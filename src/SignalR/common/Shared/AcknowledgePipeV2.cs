using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace PipelinesOverNetwork
{
    // Wrapper around a PipeReader that adds an Ack position which replaces Consumed
    // This allows the underlying pipe to keep un-acked data in the pipe while still providing only new data to the reader
    internal sealed class AckPipeReader : PipeReader
    {
        private readonly PipeReader _inner;
        private SequencePosition _consumed;
        private SequencePosition _ackPosition;
        private long _ackDiff;
        private long _ackId;
        private long _totalWritten;
        private bool _resend;
        private object _lock = new object();

        public AckPipeReader(PipeReader inner)
        {
            _inner = inner;
        }

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
            }
        }

        public void Resend()
        {
            Debug.Assert(_resend == false);
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
                _consumed = default;
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
                    //if (buffer.Slice(_ackDiff).Start.GetInteger() == 0 && buffer.Slice(_consumed).Start.GetInteger() > 0)
                    //{
                    //    Debugger.Break();
                    //}
                    //if (buffer.Slice(_consumed).Start.Equals(buffer.Slice(_ackDiff).Start))
                    //{
                    //    _consumed = buffer.Slice(_ackDiff).Start;
                    //}
                    if (buffer.Slice(_consumed).First.Length == 0 && buffer.Slice(_ackDiff).Start.GetInteger() == 0)
                    {
                        _consumed = buffer.Slice(buffer.Length - buffer.Slice(_consumed).Length).Start;
                    }
                    //buffer = buffer.Slice(_ackDiff + 16);
                    buffer = buffer.Slice(_ackDiff);
                    _ackId += _ackDiff;
                    _ackDiff = 0;
                    _ackPosition = buffer.Start;
                }
            }
            // Slice consumed, unless resending, then slice to ackPosition
            // TODO: implement resend for reconnect
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
        private const int FrameSize = 16;
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
#if NETSTANDARD2_1_OR_GREATER
            BitConverter.TryWriteBytes(_frameHeader.Span, _buffered);
            BitConverter.TryWriteBytes(_frameHeader.Slice(8).Span, lastAck);
#else
            BitConverter.GetBytes(_buffered).CopyTo(_frameHeader);
            BitConverter.GetBytes(lastAck).CopyTo(_frameHeader.Slice(8).Span);
#endif
            //Console.WriteLine($"SendingAckId: {lastAck}");
            _frameHeader = Memory<byte>.Empty;
            _buffered = 0;
            return _inner.FlushAsync(cancellationToken);
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            var segment = _inner.GetMemory(Math.Max(FrameSize + 1, sizeHint));
            if (_frameHeader.IsEmpty || _buffered == 0)
            {
                // TODO: segment less than FrameSize
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
}

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace PipelinesOverNetwork
{
    // Read from "network" 
    // Parse framing and slice the read so the application doesn't see the framing
    // Notify outbound pipe of framing details for when sending back
    internal class ParseAckPipeReader : PipeReader
    {
        private readonly PipeReader _inner;
        private readonly AckPipeWriter _ackPipeWriter;
        private readonly AckPipeReader _ackPipeReader;
        private long _totalBytes;

        private ReadOnlySequence<byte> _currentRead;

        public ParseAckPipeReader(PipeReader inner, AckPipeWriter ackPipeWriter, AckPipeReader ackPipeReader)
        {
            _inner = inner;
            _ackPipeWriter = ackPipeWriter;
            _ackPipeReader = ackPipeReader;
        }

        public override void AdvanceTo(SequencePosition consumed)
        {
            var len =_currentRead.Length - _currentRead.Slice(consumed).Length;
            //Console.WriteLine($"lastack: {_ackPipeWriter.lastAck} to {_ackPipeWriter.lastAck + len}");
            // ignore the empty length send, maybe don't return from ReadAsync instead?
            _ackPipeWriter.lastAck += (len == 16) ? 0 : len;
            _inner.AdvanceTo(consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            var len = _currentRead.Length - _currentRead.Slice(consumed).Length;
            //Console.WriteLine($"lastack: {_ackPipeWriter.lastAck} to {_ackPipeWriter.lastAck + len}");
            _ackPipeWriter.lastAck += (len == 16) ? 0 : len;
            // Track?
            _inner.AdvanceTo(consumed, examined);
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
            var res = await _inner.ReadAsync(cancellationToken);
            if (res.IsCompleted || res.IsCanceled)
            {
                if  (res.Buffer.Length >= 16)
                    res = new(res.Buffer.Slice(16), res.IsCanceled, res.IsCompleted);
                return res;
            }

            _currentRead = res.Buffer;
            // TODO: handle previous payload not fully received
            // TODO: handle multiple frame prefixed messages
            var frame = res.Buffer.Slice(0, 16);
            var len = ParseFrame(frame, _ackPipeReader);
            _totalBytes += len;
            // 0 len sent on reconnect and not part of acks
            if (len != 0)
            {
                //Console.WriteLine($"lastack: {_ackPipeWriter.lastAck} to {_ackPipeWriter.lastAck + res.Buffer.Length}");
                //_ackPipeWriter.lastAck += res.Buffer.Length;
            }

            // TODO: validation everywhere!
            Debug.Assert(len < res.Buffer.Length);

            res = new(res.Buffer.Slice(16, len), res.IsCanceled, res.IsCompleted);
            return res;

            static long ParseFrame(ReadOnlySequence<byte> frame, AckPipeReader ackPipeReader)
            {
                Span<byte> buffer = stackalloc byte[16];
                frame.CopyTo(buffer);
                // TODO: use these values
#if NETSTANDARD2_1_OR_GREATER
                var len = BitConverter.ToInt64(buffer);
                var ackId = BitConverter.ToInt64(buffer.Slice(8));
#else
                var len = BitConverter.ToInt64(buffer.Slice(0, 8).ToArray(), 0);
                var ackId = BitConverter.ToInt64(buffer.Slice(8).ToArray(), 0);
#endif
                ackPipeReader.Ack(ackId);
                return len;
            }
        }

        public override bool TryRead(out ReadResult result)
        {
            throw new NotImplementedException();
        }
    }
}

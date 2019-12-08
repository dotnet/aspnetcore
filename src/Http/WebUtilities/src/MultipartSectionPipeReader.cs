using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebUtilities
{
    internal class MultipartSectionPipeReader : PipeReader
    {
        private readonly PipeReader _pipeReader;
        private bool _finished = false;
        private bool _metadataSkipped = false;
        private readonly MultipartBoundary _boundary;
        private int _partialMatchIndex = 0;
        private ReadOnlySequence<byte> _buffer;
        private SequencePosition _endPosition;
        private bool _isReaderComplete;

        private static ReadOnlySpan<byte> CrlfDelimiter => new byte[] { (byte)'\r', (byte)'\n' };
        private static ReadOnlySpan<byte> EndOfFileDelimiter => new byte[] { (byte)'-', (byte)'-' };

        public bool CanSeek { get; }

        public long RawLength { get; private set; } = 0;

        public long? LengthLimit { get; private set; }
        public long Length { get; private set; } = 0;

        internal MultipartSectionPipeReader(PipeReader pipeReader, MultipartBoundary boundary, bool canSeek)
        {
            _pipeReader = pipeReader;
            _boundary = boundary;
            CanSeek = canSeek;
        }

        private void UpdateLength(long read)
        {
            Length += read;
            RawLength += read;
            if (LengthLimit.HasValue && Length > LengthLimit.GetValueOrDefault())
            {
                throw new InvalidDataException($"Multipart body length limit {LengthLimit.GetValueOrDefault()} exceeded.");
            }
        }

        public override Stream AsStream(bool leaveOpen = false)
        {
            return new PipeReaderStream(this, Length, leaveOpen);
        }

        public override void AdvanceTo(SequencePosition consumed)
        {
            AdvanceTo(consumed, consumed);
        }


        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {

            ThrowIfCompleted();

            if (consumed.GetObject() == null || examined.GetObject() == null)
            {
                return;
            }

            if (!_buffer.IsEmpty)
            {
                var buffer = _buffer.Slice(consumed); //not sure how to handle examined since this is relevant only at the end of the pipe.
                UpdateLength(_buffer.Length - buffer.Length);
                _buffer = buffer;
            }

            if (_metadataSkipped)
            {
                return;
            }

            if (!_finished)
            {
                _pipeReader.AdvanceTo(consumed, examined);
            }
            else if (_buffer.IsEmpty)
            {
                _pipeReader.AdvanceTo(_endPosition);

            }
            else
            {
                _pipeReader.AdvanceTo(consumed);
            }
        }

        private void ThrowIfCompleted()
        {
            if (_isReaderComplete)
            {
                throw new InvalidOperationException("No Reading Allowed");
            }
        }

        public override void CancelPendingRead()
        {
            //todo maybe handle cancellation of a finished Read
            _pipeReader.CancelPendingRead();
        }

        public override void Complete(Exception exception = null)
        {
            _isReaderComplete = true;
            _pipeReader.Complete(exception);
        }

        public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {

            ThrowIfCompleted();

            if (!_finished)
            {
                var readResult = await _pipeReader.ReadAsync(cancellationToken);
                _buffer = readResult.Buffer;
                var buffer = readResult.Buffer;
                (var didReachEnd, var read) = TryAdvanceToEnd(ref buffer, readResult.IsCompleted);
                _buffer = _buffer.Slice(0, read);
                if (didReachEnd)
                {
                    _finished = true;
                    _pipeReader.AdvanceTo(_buffer.Start, buffer.Start);
                    _endPosition = buffer.Start;
                    return new ReadResult(_buffer, readResult.IsCanceled, false);
                }
                if (buffer.IsEmpty)
                {
                    return readResult;
                }

                if (readResult.IsCompleted)
                {
                    throw new IOException("Unexpected end of Stream, the content may have already been read by another component. ");
                }
                return new ReadResult(_buffer, readResult.IsCanceled, false);

            }
            if (!_buffer.IsEmpty)
            {
                return new ReadResult(_buffer, false, false);
            }
            if (!_metadataSkipped)
            {
                var readResult = await _pipeReader.ReadAsync();
                var sequence = readResult.Buffer;
                var isCurrentCompleted = TrySkipMetadata(ref sequence, readResult.IsCompleted);
                if (isCurrentCompleted)
                {
                    _pipeReader.AdvanceTo(sequence.Start);
                    _metadataSkipped = true;
                }
                else
                {
                    _pipeReader.AdvanceTo(sequence.Start, sequence.End);
                }

                if (readResult.IsCompleted && !isCurrentCompleted)
                {
                    throw new IOException("Unexpected end of Stream, the content may have already been read by another component. ");
                }

                return new ReadResult(default, false, isCurrentCompleted);
            }

            //currently there is no handling of a finished read cancellation, so we set false for isCanceled
            return new ReadResult(_buffer, false, true);
        }

        public override bool TryRead(out ReadResult result)
        {
            if (!_finished)
            {
                if (!_pipeReader.TryRead(out result))
                {
                    return false;
                }

                _buffer = result.Buffer;
                var buffer = result.Buffer;
                (var didReachEnd, var read) = TryAdvanceToEnd(ref buffer, result.IsCompleted);
                _buffer = _buffer.Slice(0, read);
                if (didReachEnd)
                {
                    _finished = true;
                    _pipeReader.AdvanceTo(_buffer.Start, buffer.Start);
                    _endPosition = buffer.Start;
                    result = new ReadResult(_buffer, result.IsCanceled, false);
                    return true;
                }
                if (buffer.IsEmpty)
                {
                    return true;
                }

                if (result.IsCompleted)
                {
                    throw new IOException("Unexpected end of Stream, the content may have already been read by another component. ");
                }
                result = new ReadResult(_buffer, result.IsCanceled, false);
                return true;

            }
            if (!_buffer.IsEmpty)
            {
                result = new ReadResult(_buffer, false, false);
                return true;
            }
            if (!_metadataSkipped)
            {
                if (!_pipeReader.TryRead(out result))
                {
                    return false;
                }

                var sequence = result.Buffer;
                var isCurrentCompleted = TrySkipMetadata(ref sequence, result.IsCompleted);
                if (isCurrentCompleted)
                {
                    _pipeReader.AdvanceTo(sequence.Start);
                    _metadataSkipped = true;
                }
                else
                {
                    _pipeReader.AdvanceTo(sequence.Start, sequence.End);
                }

                if (result.IsCompleted && !isCurrentCompleted)
                {
                    throw new IOException("Unexpected end of Stream, the content may have already been read by another component. ");
                }

                result = new ReadResult(default, false, isCurrentCompleted);
                return true;
            }

            //currently there is no handling of a finished read cancellation, so we set false for isCanceled
            result = new ReadResult(_buffer, false, true);
            return true;
        }

        private (bool reachedEnd, long consumed) TryAdvanceToEnd(ref ReadOnlySequence<byte> sequence, bool isFinalBlock)
        {
            var sequenceReader = new SequenceReader<byte>(sequence);
            long read = 0;

            if (isFinalBlock && sequence.Length < _boundary.FinalBoundaryLength - _partialMatchIndex)
            {
                throw new IOException("Unexpected end of Stream, the content may have already been read by another component. ");
            }

            while (!sequenceReader.End)
            {
                if (!sequenceReader.TryReadTo(out ReadOnlySequence<byte> body, _boundary.BoundaryBytes[_partialMatchIndex]))
                {
                    //advance reader to end
                    sequence = sequence.Slice(sequence.End);
                    return (false, sequenceReader.Length);
                }

                read += body.Length;

                if (_partialMatchIndex != 0 && body.Length > 0)
                {
                    // there was a potential end, but it actually isn't
                    read += _partialMatchIndex; //include *previous* matches as read
                    _partialMatchIndex = 0;
                    sequence = sequence.Slice(sequenceReader.Consumed - 1); // the current _partialMatchIndex might be a begining to a boundary
                    return (false, read);
                }

                //continue to check for boundaryMatch
                for (int i = _partialMatchIndex + 1; i < _boundary.BoundaryBytes.Length; i++)
                {
                    if (sequenceReader.TryRead(out var value))
                    {
                        if (_boundary.BoundaryBytes[i] == value)
                        {
                            continue;
                        }

                        //no match - mark previous matches as read, the current one might be the begining of a new boundary
                        read += i;
                        sequence = sequence.Slice(sequenceReader.Consumed - 1);
                        return (false, read);

                    }

                    //end of sequence
                    //There still might be a partial Match, we need to read more to know for sure
                    _partialMatchIndex = i;
                    sequence = sequence.Slice(read);
                    //also slice partial read to insure _bufferedData is empty in the next check
                    sequence = sequence.Slice(i);
                    return (false, read);
                }

                //boundary was found
                sequence = sequence.Slice(sequenceReader.Position);
                RawLength += _boundary.BoundaryBytes.Length;
                return (true, read);
            }
            return (false, read);
        }

        private bool TrySkipMetadata(ref ReadOnlySequence<byte> sequence, bool isFinalBlock)
        {
            var sequenceReader = new SequenceReader<byte>(sequence);

            while (!sequenceReader.End)
            {
                if (!sequenceReader.TryReadTo(out var body, CrlfDelimiter))
                {
                    if (sequenceReader.TryReadTo(out var _, EndOfFileDelimiter))
                    {
                        sequence = sequence.Slice(sequenceReader.Position);
                        RawLength += sequenceReader.Consumed;
                        Debug.Assert(sequenceReader.End, "Un-expected data found on the boundary line");

                        return true;
                    }

                    if (!isFinalBlock)
                    {
                        // Don't buffer indefinately
                        if (sequenceReader.Consumed > 100)
                        {
                            Debug.Print("Un-expected data found on the boundary line: " + body);
                            sequence = sequence.Slice(sequenceReader.Consumed);
                            RawLength += sequenceReader.Consumed;
                            return true;
                        }
                        sequence = sequence.Slice(sequence.End);
                        return false;
                    }
                }

                sequence = sequence.Slice(sequenceReader.Position);
                RawLength += sequenceReader.Consumed;
                return true;

            }
            sequence = sequence.Slice(sequenceReader.Position);
            RawLength += sequenceReader.Consumed;
            return false;
        }
    }
}

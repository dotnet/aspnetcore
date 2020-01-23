// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

        // indicates that we finished reading the body of the multipart section - meaning we reached the boundary bytes
        private bool _finished = false;

        //indicates that the underlying pipeReader passed the ending metadata (either newline or --) after the boundary bytes.
        private bool _metadataSkipped = false;

        private readonly MultipartBoundary _boundary;

        // indicates the last index that we managed to match in boundaryBytes
        private int _partialMatchIndex = 0;
        private ReadOnlySequence<byte> _bodyBuffer;

        //indicates the positing of the end of the boundary bytes.
        private SequencePosition _endPosition;
        private bool _isReaderComplete;

        private static ReadOnlySpan<byte> CrlfDelimiter => new byte[] { (byte)'\r', (byte)'\n' };
        private static ReadOnlySpan<byte> EndOfFileDelimiter => new byte[] { (byte)'-', (byte)'-' };

        public long RawLength { get; private set; } = 0;

        public long? LengthLimit { get; private set; }

        internal MultipartSectionPipeReader(PipeReader pipeReader, MultipartBoundary boundary)
        {
            _pipeReader = pipeReader;
            _boundary = boundary;
        }

        private void UpdateLength(long read)
        {
            RawLength += read;
            if (LengthLimit.HasValue && RawLength > LengthLimit.GetValueOrDefault())
            {
                throw new InvalidDataException($"Multipart body length limit {LengthLimit.GetValueOrDefault()} exceeded.");
            }
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

            if (!_bodyBuffer.IsEmpty)
            {
                var buffer = _bodyBuffer.Slice(consumed); //not sure how to handle examined since this is relevant only at the end of the pipe.
                UpdateLength(_bodyBuffer.Length - buffer.Length);
                _bodyBuffer = buffer;
            }

            if (_metadataSkipped)
            {
                return;
            }

            if (!_finished)
            {
                //we didn't reach the boundary bytes in the underlying buffer, we want to enusre the next read advances further than we already have
                _pipeReader.AdvanceTo(consumed, examined);
            }
            else if (_bodyBuffer.IsEmpty)
            {
                //finished == true &&  _bodyBuffer.IsEmpty - user advanced to the end of the Section Body. We will advance the pipe further, to reach the end of the boundary bytes.
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
                _bodyBuffer = readResult.Buffer;
                var parsingBuffer = readResult.Buffer;
                (var didReachEnd, var read) = TryAdvanceToBoundaryBytes(ref parsingBuffer, readResult.IsCompleted);
                _bodyBuffer = _bodyBuffer.Slice(0, read);
                if (didReachEnd)
                {
                    _finished = true;
                    _pipeReader.AdvanceTo(_bodyBuffer.Start, parsingBuffer.Start);
                    _endPosition = parsingBuffer.Start;
                    return new ReadResult(_bodyBuffer, readResult.IsCanceled, false);
                }
                if (parsingBuffer.IsEmpty)
                {
                    return readResult;
                }

                if (readResult.IsCompleted)
                {
                    throw new IOException("Unexpected end of Stream, the content may have already been read by another component. ");
                }
                return new ReadResult(_bodyBuffer, readResult.IsCanceled, false);

            }
            if (!_bodyBuffer.IsEmpty)
            {
                return new ReadResult(_bodyBuffer, false, false);
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

                return new ReadResult(default, readResult.IsCanceled, isCurrentCompleted);
            }

            //currently there is no handling of a finished read cancellation, so we set false for isCanceled
            return new ReadResult(_bodyBuffer, false, true);
        }

        public override bool TryRead(out ReadResult result)
        {
            if (!_finished)
            {
                if (!_pipeReader.TryRead(out result))
                {
                    return false;
                }

                _bodyBuffer = result.Buffer;
                var buffer = result.Buffer;
                (var didReachEnd, var read) = TryAdvanceToBoundaryBytes(ref buffer, result.IsCompleted);
                _bodyBuffer = _bodyBuffer.Slice(0, read);
                if (didReachEnd)
                {
                    _finished = true;
                    _pipeReader.AdvanceTo(_bodyBuffer.Start, buffer.Start);
                    _endPosition = buffer.Start;
                    result = new ReadResult(_bodyBuffer, result.IsCanceled, false);
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
                result = new ReadResult(_bodyBuffer, result.IsCanceled, false);
                return true;

            }
            if (!_bodyBuffer.IsEmpty)
            {
                result = new ReadResult(_bodyBuffer, false, false);
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

                result = new ReadResult(default, result.IsCanceled, isCurrentCompleted);
                return true;
            }

            //currently there is no handling of a finished read cancellation, so we set false for isCanceled
            result = new ReadResult(_bodyBuffer, false, true);
            return true;
        }

        /// <summary>
        /// Scan for matches to boundary bytes - updates sequence to after the partial or full match
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="isFinalBlock"></param>
        /// <returns>return true only if found a full match</returns>
        private (bool reachedEnd, long consumed) TryAdvanceToBoundaryBytes(ref ReadOnlySequence<byte> sequence, bool isFinalBlock)
        {
            var sequenceReader = new SequenceReader<byte>(sequence);
            long read = 0;

            if (isFinalBlock && sequence.Length < _boundary.FinalBoundaryLength - _partialMatchIndex)
            {
                throw new IOException("Unexpected end of Stream, the content may have already been read by another component. ");
            }

            while (!sequenceReader.End)
            {
                // read until the first byte of the boundaryBytes
                if (!sequenceReader.TryReadTo(out ReadOnlySequence<byte> body, _boundary.BoundaryBytes[_partialMatchIndex]))
                {
                    //no match - end wasn't reached. Advance to end.
                    sequence = sequence.Slice(sequence.End);
                    return (false, sequenceReader.Length);
                }

                read += body.Length;

                if (_partialMatchIndex != 0 && body.Length > 0)
                {
                    // there was a potential end, but it actually isn't
                    read += _partialMatchIndex; //include *previous* matches as read
                    _partialMatchIndex = 0;
                    sequence = sequence.Slice(sequenceReader.Consumed - 1); // the current _partialMatchIndex might still be a begining to a boundary
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

        /// <summary>
        /// Skips the following metadata - 
        /// "The boundary may be followed by zero or more characters of
        /// linear whitespace. It is then terminated by either another CRLF
        /// or -- for the final boundary."
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="isFinalBlock"></param>
        /// <returns></returns>
        private bool TrySkipMetadata(ref ReadOnlySequence<byte> sequence, bool isFinalBlock)
        {
            var sequenceReader = new SequenceReader<byte>(sequence);

            bool reachedNewLine = sequenceReader.TryReadTo(out var remainder, CrlfDelimiter);
            // Don't buffer indefinitely
            if (sequenceReader.Consumed > 100)
            {
                _pipeReader.AdvanceTo(sequence.Start, sequence.End);
                throw new InvalidDataException($"Line length limit 100 exceeded.");
            }

            // Check if we reached "--" for the final boundary, and that there is only whitespace left
            var remainderReader = new SequenceReader<byte>(remainder);
            // Whitespace consists of space or horizontal tab
            remainderReader.AdvancePastAny(0x20, 0x09);
            if (!remainderReader.End)
            {
                if (!remainderReader.TryReadTo(out var buffer, EndOfFileDelimiter) || buffer.Length != 0)
                {
                    throw new InvalidDataException("Un-expected data found on the boundary line");
                }

                remainderReader.AdvancePastAny(0x20, 0x09);
                if (!remainderReader.End)
                {
                    throw new InvalidDataException("Un-consumed data found on the boundary line");
                }
            }

            // there is still more that can be read before the end
            if (!reachedNewLine && !isFinalBlock)
            {
                sequence = sequence.Slice(sequence.End);
                return false;
            }

            sequence = sequence.Slice(sequenceReader.Position);
            RawLength += sequenceReader.Consumed;
            return true;

        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebUtilities
{
    internal class MultipartSectionPipeReader : PipeReader
    {
        private static ReadOnlySpan<byte> CrlfDelimiter => new byte[] { (byte)'\r', (byte)'\n' };
        private static ReadOnlySpan<byte> EndOfFileDelimiter => new byte[] { (byte)'-', (byte)'-' };

        private readonly PipeReader _pipeReader;

        private readonly MultipartBoundary _boundary;

        // Data available to the caller.
        private ReadOnlySequence<byte> _bodyBuffer;
        // Data not yet given to the caller.
        private ReadOnlySequence<byte> _unconsumedData;

        // If AdvanceTo must be called before ReadAsync can be called again.
        private bool _advanceNeeded = false;

        // Indicates that we finished reading the body of the multipart section - meaning we reached the boundary bytes.
        private bool _boundaryFound = false;

        private bool _endOfSectionFound = false;

        // We have advanced past all of the trailing metadata, the section is fully complete.
        private bool _finalLineConsumed = false;

        // The length of the boundary + metadata at the end of the section.
        private long _finalLineLength;

        private bool _isReaderComplete;

        // The count of data fully consumed by the caller.
        private long _consumedLength;

        public long? LengthLimit { get; set; }

        // This was the last section in the multipart form
        public bool FinalBoundaryFound { get; private set; }

        internal MultipartSectionPipeReader(PipeReader pipeReader, MultipartBoundary boundary)
        {
            _pipeReader = pipeReader;
            _boundary = boundary;
        }

        public override void AdvanceTo(SequencePosition consumed)
        {
            AdvanceTo(consumed, consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            ThrowIfCompleted();

            if (consumed.GetObject() == null)
            {
                return;
            }

            if (!_advanceNeeded)
            {
                throw new InvalidOperationException("AdvanceTo can only be called once per read operation.");
            }

            if (examined.Equals(_bodyBuffer.End))
            {
                // The caller has seen all of the available data.
                if (!_boundaryFound)
                {
                    // We may have a partial boundary match in the
                    // unconsumed buffer and we need more data to continue.
                    _pipeReader.AdvanceTo(consumed, _unconsumedData.End);
                }
                else
                {
                    // We reached the end of the section, we should advance to the end.
                    _pipeReader.AdvanceTo(_unconsumedData.End);
                    _finalLineConsumed = true;
                }
            }
            else
            {
                _pipeReader.AdvanceTo(consumed, examined);
            }

            _advanceNeeded = false;

            if (!_bodyBuffer.IsEmpty)
            {
                var unconsumed = _bodyBuffer.Slice(consumed);
                _consumedLength += _bodyBuffer.Length - unconsumed.Length;
                _bodyBuffer = unconsumed;
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
            _pipeReader.CancelPendingRead();
        }

        public override void Complete(Exception? exception = null)
        {
            _isReaderComplete = true;
            _pipeReader.Complete(exception);
        }

        public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfCompleted();

            if (_advanceNeeded)
            {
                throw new InvalidOperationException("AdvanceTo was not called after the last read operation.");
            }

            // TODO: Refactor.
            // - It's fishy that today we do not return isCompleted until the caller as consumed all of the data we've given them. That's not normal pipe contract.
            // - When we've found the boundary also scan for the trailing metadata.
            // - Return isCompleted true if we've found the end of the metadata. Track the boundary and metadata in _unconsumedData.
            // - Advance to _unconsumedData.End if the isCompleted and the caller advances to _bodyBuffer.End.

            while (!_endOfSectionFound)
            {
                var priorDataLength = _bodyBuffer.Length;
                var readResult = await _pipeReader.ReadAsync(cancellationToken);

                if (readResult.IsCanceled)
                {
                    return new ReadResult(default, isCanceled: true, isCompleted: false);
                }

                // Simple case, we haven't positively identified the end yet, look for it again.
                if (!_boundaryFound)
                {
                    _unconsumedData = readResult.Buffer;
                    _bodyBuffer = default;
                    _boundaryFound = TryReadToBoundaryBytes(readResult.IsCompleted);
                }
                else
                {
                    // We already know where the body ends, but we still need to find the end of the section.
                    // We can't assume we got back the same buffers as before, only the same data. Refresh the buffers.
                    _bodyBuffer = readResult.Buffer.Slice(0, priorDataLength);
                    _unconsumedData = readResult.Buffer.Slice(priorDataLength);
                }

                // We still didn't find it, but we've got some new data we could return.
                if (!_boundaryFound && _bodyBuffer.Length > priorDataLength)
                {
                    if (LengthLimit.HasValue && LengthLimit.Value - _consumedLength < _bodyBuffer.Length)
                    {
                        _pipeReader.AdvanceTo(_unconsumedData.End);
                        throw new InvalidDataException($"Multipart section body length limit {LengthLimit.GetValueOrDefault()} exceeded.");
                    }

                    _advanceNeeded = true;
                    // We want them to consume and advance all of the data before we try to drain the metadata / boundary.
                    return new ReadResult(_bodyBuffer, readResult.IsCanceled, isCompleted: false);
                }

                if (!_boundaryFound)
                {
                    // We must be examining a partial boundary match, keep reading.
                    _pipeReader.AdvanceTo(readResult.Buffer.Start, _unconsumedData.End);
                    continue;
                }

                // We've found the boundary. Do we have the trailing metadata now?
                _endOfSectionFound = TryReadToEndOfSection(readResult.IsCompleted);

                if (!_endOfSectionFound)
                {
                    // We need more data find the end of the section
                    _pipeReader.AdvanceTo(readResult.Buffer.Start, _unconsumedData.End);
                    continue;
                }

                _advanceNeeded = true; //!_bodyBuffer.IsEmpty;
                return new ReadResult(_bodyBuffer, readResult.IsCanceled, isCompleted: true);
            }

            var remainingBodyLength = _bodyBuffer.Length;
            if (remainingBodyLength > 0)
            {
                // We've found the end of the section but the caller still has data to consume.
                // Get a fresh read so we can advance again.
                var readResult = await _pipeReader.ReadAsync(cancellationToken);
                // We can't assume we got back the same buffers as before, only the same data. Refresh the buffers.
                _bodyBuffer = readResult.Buffer.Slice(0, remainingBodyLength);
                _unconsumedData = readResult.Buffer.Slice(remainingBodyLength);

                _advanceNeeded = true;
                return new ReadResult(_bodyBuffer, readResult.IsCanceled, isCompleted: true);
            }

            if (!_finalLineConsumed)
            {
                // TODO: This should have been covered by advance, unless the body was empty?
                var finalLine = _unconsumedData.Slice(0, _finalLineLength);
                _pipeReader.AdvanceTo(finalLine.End);
                _finalLineConsumed = true;
            }

            return new ReadResult(default, isCanceled: false, isCompleted: true);
        }

        public override bool TryRead(out ReadResult result)
        {
            ThrowIfCompleted();

            if (_advanceNeeded)
            {
                throw new InvalidOperationException("AdvanceTo was not called after the last read operation.");
            }

            result = default;

            if (!_boundaryFound)
            {
                if (!_pipeReader.TryRead(out var readResult))
                {
                    return false;
                }

                _unconsumedData = readResult.Buffer;
                _bodyBuffer = default;
                _boundaryFound = TryReadToBoundaryBytes(readResult.IsCompleted);

                if (!_boundaryFound && readResult.IsCompleted)
                {
                    _pipeReader.AdvanceTo(_unconsumedData.End);
                    throw new IOException("Unexpected end of Stream, the content may have already been read by another component.");
                }

                if (!_bodyBuffer.IsEmpty)
                {
                    if (LengthLimit.HasValue && LengthLimit.Value - _consumedLength < _bodyBuffer.Length)
                    {
                        _pipeReader.AdvanceTo(_unconsumedData.End);
                        throw new InvalidDataException($"Multipart body length limit {LengthLimit.GetValueOrDefault()} exceeded.");
                    }

                    _advanceNeeded = true;
                    // We want them to consume and advance all of the data before we try to drain the metadata / boundary.
                    result = new ReadResult(_bodyBuffer, readResult.IsCanceled, isCompleted: false);
                    return true;
                }

                return false;
            }

            if (!_bodyBuffer.IsEmpty)
            {
                // We need another read so the caller can advance remaining data.
                // No need to re-parse, _bodyBuffer already tracks all remaining message data.
                if (!_pipeReader.TryRead(out var readResult))
                {
                    // How did this happen?
                    return false;
                }

                _advanceNeeded = true;

                // We can't assume we got back the same buffers as before, only the same data. Refresh the buffers.
                _bodyBuffer = readResult.Buffer.Slice(0, _bodyBuffer.Length);
                _unconsumedData = readResult.Buffer.Slice(_bodyBuffer.Length);

                // We want the caller to consume and advance all of the data before we try to drain the metadata / boundary.
                result = new ReadResult(_bodyBuffer, isCanceled: readResult.IsCanceled, isCompleted: false);
                return true;
            }

            // The caller has advanced past all message data. The boundary has been found but not advanced past yet.
            // We'll handle all further advances internally.
            if (!_finalLineConsumed)
            {
                if (!_pipeReader.TryRead(out var readResult))
                {
                    // How did this happen, the boundary should already be there?
                    return false;
                }

                _unconsumedData = readResult.Buffer;

                var isSectionCompleted = TryReadToEndOfSection(readResult.IsCompleted);
                if (isSectionCompleted)
                {
                    _pipeReader.AdvanceTo(_unconsumedData.Start);
                    _finalLineConsumed = true;
                    return true;
                }
                else
                {
                    _pipeReader.AdvanceTo(_unconsumedData.Start, _unconsumedData.End);
                }

                if (readResult.IsCompleted && !isSectionCompleted)
                {
                    throw new IOException("Unexpected end of Stream, the content may have already been read by another component. ");
                }

                return false;
            }

            result = new ReadResult(buffer: default, isCanceled: false, isCompleted: true);
            return true;
        }

        /// <summary>
        /// Scan for matches to boundary bytes - updates sequence to after the partial or full match
        /// </summary>
        /// <param name="isFinalBlock"></param>
        /// <returns>return true only if found a full match</returns>
        private bool TryReadToBoundaryBytes(bool isFinalBlock)
        {
            if (isFinalBlock && _unconsumedData.Length < _boundary.FinalBoundaryLength)
            {
                _pipeReader.AdvanceTo(_unconsumedData.End);
                throw new IOException("Unexpected end of Stream, the content may have already been read by another component. ");
            }

            if (_unconsumedData.Length < _boundary.BoundaryBytes.Length)
            {
                // Don't even try to match, wait for enough data.
                return false;
            }

            var sequenceReader = new SequenceReader<byte>(_unconsumedData);
            if (sequenceReader.TryReadTo(out ReadOnlySequence<byte> body, _boundary.BoundaryBytes, advancePastDelimiter: false))
            {
                // Found a full match.
                _bodyBuffer = body;
                _unconsumedData = _unconsumedData.Slice(_bodyBuffer.End);
                return true;
            }

            // We can't return any trailing bytes that might be part of the boundary.
            // Rather than trying to do complex submatches, just return the data we're sure about and
            // wait for more.
            var consumed = _unconsumedData.Length - _boundary.BoundaryBytes.Length;
            sequenceReader.Advance(consumed);
            if (sequenceReader.TryAdvanceTo(_boundary.BoundaryBytes[0], advancePastDelimiter: false))
            {
                // Possible boundary match
                _bodyBuffer = _unconsumedData.Slice(0, sequenceReader.Position);
                _unconsumedData = _unconsumedData.Slice(sequenceReader.Position);
            }
            else
            {
                // No boundary data, all message data.
                _bodyBuffer = _unconsumedData;
                _unconsumedData = _unconsumedData.Slice(_unconsumedData.Length);
            }
            return false;
        }

        /// <summary>
        /// Called after the boundary has been found. Scans for any trailing metadata.
        /// "The boundary may be followed by zero or more characters of
        /// linear whitespace. It is then terminated by either another CRLF
        /// or -- for the final boundary."
        /// </summary>
        /// <param name="isFinalBlock"></param>
        /// <returns></returns>
        private bool TryReadToEndOfSection(bool isFinalBlock)
        {
            var sequenceReader = new SequenceReader<byte>(_unconsumedData);
            // The unconsumed data begins with the boundary.
            sequenceReader.Advance(_boundary.BoundaryBytes.Length);

            var reachedNewLine = sequenceReader.TryReadTo(out ReadOnlySequence<byte> remainder, CrlfDelimiter, advancePastDelimiter: true);
            // Don't buffer indefinitely
            if (sequenceReader.Consumed > 100 + _boundary.BoundaryBytes.Length)
            {
                _pipeReader.AdvanceTo(_unconsumedData.Start);
                throw new InvalidDataException($"Metadata line length limit 100 exceeded.");
            }

            if (!reachedNewLine && !isFinalBlock)
            {
                // Need more data
                return false;
            }

            // Some formatters leave off the final CRLF, take the remaining data as the last line.
            if (!reachedNewLine && isFinalBlock)
            {
                remainder = _unconsumedData.Slice(sequenceReader.Position); // Minus boundary
            }

            // Check if we reached "--" for the final boundary, and that there is only whitespace left
            var remainderReader = new SequenceReader<byte>(remainder);

            // Whitespace consists of space or horizontal tab
            remainderReader.AdvancePastAny(0x20, 0x09);

            // Maybe the final body terminator "--"?
            if (!remainderReader.End)
            {
                // The only non-whitespace characters allowed at this point are the "--" terminators.
                if (!remainderReader.TryReadTo(out ReadOnlySequence<byte> buffer, EndOfFileDelimiter) || buffer.Length != 0)
                {
                    _pipeReader.AdvanceTo(sequenceReader.Position);
                    throw new InvalidDataException("Un-expected data found on the boundary line");
                }

                FinalBoundaryFound = true;

                // A little more whitespace
                remainderReader.AdvancePastAny(0x20, 0x09);
                if (!remainderReader.End)
                {
                    _pipeReader.AdvanceTo(sequenceReader.Position);
                    throw new InvalidDataException("Un-consumed data found on the boundary line");
                }
            }

            _unconsumedData = _unconsumedData.Slice(0, sequenceReader.Position);
            _finalLineLength = _unconsumedData.Length;

            return true;
        }
    }
}

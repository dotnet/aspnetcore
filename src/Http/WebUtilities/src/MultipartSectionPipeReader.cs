using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebUtilities
{
    public class MultipartSectionPipeReader
    {
        private readonly PipeReader _pipeReader;
        private bool _finished = false;
        private bool _metadataSkipped = false;
        private readonly MultipartBoundary _boundary;
        private int _partialMatchIndex = 0;
        private const int StackAllocThreshold = 128;
        private ReadOnlySequence<byte> _buffer;

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

        public int Read(byte[] buffer, int offset, int count)
        {
            if (_finished && _metadataSkipped)
            {
                return 0;
            }
            int consumed = 0;
            ReadOnlySequence<byte> sequence = default;
            ReadResult readResult = default;
            while (true)
            {
                if (!sequence.IsEmpty)
                {
                    if (!_finished)
                    {
                        var (didReachEnd, copied) = TryCopyToEnd(ref sequence, buffer, offset, count, readResult.IsCompleted);
                        _pipeReader.AdvanceTo(sequence.Start);
                        consumed += copied;
                        if (didReachEnd)
                        {
                            _finished = true;
                            Length += copied;
                            RawLength += copied;
                        }

                        if (copied > buffer.Length - offset)
                        {
                            Length += copied;
                            RawLength += copied;
                            return consumed;
                        }
                        offset += copied;
                    }
                    else if (!_metadataSkipped)
                    {
                        if (TrySkipMetadata(ref sequence, readResult.IsCompleted))
                        {
                            _pipeReader.AdvanceTo(sequence.Start);
                            _metadataSkipped = true;
                            return consumed;
                        }
                    }
                    else
                    {
                        return consumed;
                    }
                }

                if (_pipeReader.TryRead(out readResult))
                {
                    if (readResult.IsCompleted)
                    {
                        _finished = true;
                        _metadataSkipped = true;
                        //handle last result
                        return consumed;
                    }

                    sequence = readResult.Buffer;
                }
                else
                {
                    return 0;
                }
            }
        }

        internal async Task<string> ReadToEndAsync(Encoding streamEncoding, CancellationToken cancellationToken = default)
        {
            string GetDecodedString(ReadOnlySpan<byte> readOnlySpan)
            {
                if (readOnlySpan.Length == 0)
                {
                    return string.Empty;
                }
                else
                {
                    // We need to create a Span from a ReadOnlySpan. This cast is safe because the memory is still held by the pipe
                    // We will also create a string from it by the end of the function.
                    var span = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(readOnlySpan[0]), readOnlySpan.Length);
                    return streamEncoding.GetString(span);
                }
            }

            string GetDecodedStringFromReadOnlySequence(in ReadOnlySequence<byte> ros)
            {
                if (ros.IsSingleSegment)
                {
                    return GetDecodedString(ros.First.Span);
                }

                if (ros.Length < StackAllocThreshold)
                {
                    Span<byte> buffer = stackalloc byte[(int)ros.Length];
                    ros.CopyTo(buffer);
                    return GetDecodedString(buffer);
                }
                else
                {
                    var byteArray = ArrayPool<byte>.Shared.Rent((int)ros.Length);

                    try
                    {
                        Span<byte> buffer = byteArray.AsSpan(0, (int)ros.Length);
                        ros.CopyTo(buffer);
                        return GetDecodedString(buffer);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(byteArray);
                    }
                }
            }

            if (_finished && _metadataSkipped)
            {
                return "";
            }

            long consumed = 0;
            var stringBuilder = new StringBuilder();
            ReadOnlySequence<byte> sequence = default;
            ReadResult readResult = default;
            while (true)
            {
                if (!sequence.IsEmpty)
                {
                    if (!_finished)
                    {
                        var tempSequence = sequence;
                        (bool didReachEnd, long read) = TryAdvanceToEnd(ref tempSequence, readResult.IsCompleted);
                        stringBuilder.Append(GetDecodedStringFromReadOnlySequence(sequence.Slice(0, read)));
                        _pipeReader.AdvanceTo(tempSequence.Start);
                        UpdateLength(read);
                        consumed += read;
                        if (didReachEnd)
                        {
                            _finished = true;
                        }
                    }
                    else if (!_metadataSkipped)
                    {
                        var result = TrySkipMetadata(ref sequence, readResult.IsCompleted);
                        _pipeReader.AdvanceTo(sequence.Start);
                        if (result)
                        {
                            _metadataSkipped = true;
                            return stringBuilder.ToString();
                        }
                    }
                }
                else if (readResult.IsCompleted)
                {
                    throw new IOException("Unexpected end of Stream, the content may have already been read by another component. ");
                }

                readResult = await _pipeReader.ReadAsync(cancellationToken);
                sequence = readResult.Buffer;
            }
        }

        public async Task DrainAsync(CancellationToken cancellationToken)
        {
            if (_finished && _metadataSkipped)
            {
                return;
            }

            long consumed = 0;
            ReadOnlySequence<byte> sequence = default;
            ReadResult readResult = default;
            while (true)
            {
                if (!sequence.IsEmpty)
                {
                    if (!_finished)
                    {
                        (bool didReachEnd, long read) = TryAdvanceToEnd(ref sequence, readResult.IsCompleted);
                        _pipeReader.AdvanceTo(sequence.Start);
                        UpdateLength(read);
                        consumed += read;
                        if (didReachEnd)
                        {
                            _finished = true;
                        }
                    }
                    else if (!_metadataSkipped)
                    {
                        var result = TrySkipMetadata(ref sequence, readResult.IsCompleted);
                        _pipeReader.AdvanceTo(sequence.Start);
                        if (result)
                        {
                            _metadataSkipped = true;
                            return;
                        }
                    }
                }
                else if (readResult.IsCompleted)
                {
                    throw new IOException("Unexpected end of Stream, the content may have already been read by another component. ");
                }

                readResult = await _pipeReader.ReadAsync(cancellationToken);
                sequence = readResult.Buffer;
            }
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (_finished && _metadataSkipped)
            {
                return 0;
            }
            if (_buffer.Length > count || _buffer.Length > buffer.Length - offset)
            {
                var read = CopySequenceToBuffer(_buffer, buffer, offset, count);
                _buffer = _buffer.Slice(read);
                _pipeReader.AdvanceTo(_buffer.Start);
                return read;
            }

            long consumed = 0;

            if (_buffer.Length > 0)
            {
                consumed = CopySequenceToBuffer(_buffer, buffer, offset, count);
                _buffer = default;
            }

            ReadResult readResult = default;
            while (true)
            {
                if (!_buffer.IsEmpty)
                {
                    if (!_finished)
                    {
                        var localSequence = _buffer;
                        (bool didReachEnd, long read) = TryAdvanceToEnd(ref localSequence, readResult.IsCompleted);
                        _buffer = _buffer.Slice(0, read);
                        read = CopySequenceToBuffer(_buffer, buffer, offset, count);
                        _buffer = _buffer.Slice(read);
                        _pipeReader.AdvanceTo(localSequence.Start);
                        UpdateLength(read);
                        offset += (int)read;
                        consumed += read;
                        if (didReachEnd)
                        {
                            _finished = true;
                        }
                        return (int)consumed;

                    }
                    else if (!_metadataSkipped)
                    {
                        var result = TrySkipMetadata(ref _buffer, readResult.IsCompleted);
                        _pipeReader.AdvanceTo(_buffer.Start);
                        if (result)
                        {
                            _metadataSkipped = true;
                            return (int)consumed;
                        }
                    }
                }
                else if (readResult.IsCompleted)
                {
                    throw new IOException("Unexpected end of Stream, the content may have already been read by another component. ");
                }

                readResult = await _pipeReader.ReadAsync(cancellationToken);
                _buffer = readResult.Buffer;
            }
        }


        //public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        //{
        //    if (_finished && _metadataSkipped)
        //    {
        //        return 0;
        //    }
        //    int consumed = 0;
        //    ReadOnlySequence<byte> sequence = default;
        //    ReadResult readResult = default;
        //    while (true)
        //    {
        //        if (!sequence.IsEmpty)
        //        {
        //            if (!_finished)
        //            {
        //                var (didReachEnd, copied) = TryCopyToEnd(ref sequence, buffer, offset, count, readResult.IsCompleted);
        //                _pipeReader.AdvanceTo(sequence.Start);
        //                UpdateLength(copied);
        //                consumed += copied;
        //                if (didReachEnd)
        //                {
        //                    _finished = true;
        //                }
        //                else if (!sequence.IsEmpty)
        //                {
        //                    return consumed;
        //                }

        //                if (copied >= buffer.Length - offset || copied >= count)
        //                {
        //                    return consumed;
        //                }
        //                offset += copied;
        //            }
        //            else if (!_metadataSkipped)
        //            {
        //                var result = TrySkipMetadata(ref sequence, readResult.IsCompleted);
        //                _pipeReader.AdvanceTo(sequence.Start);
        //                if (result)
        //                {
        //                    _metadataSkipped = true;
        //                    return consumed;
        //                }
        //            }
        //        }
        //        else if (readResult.IsCompleted)
        //        {
        //            throw new IOException("Unexpected end of Stream, the content may have already been read by another component. ");
        //        }

        //        readResult = await _pipeReader.ReadAsync(cancellationToken);
        //        sequence = readResult.Buffer;
        //    }
        //}

        private void UpdateLength(long read)
        {
            Length += read;
            RawLength += read;
            if (LengthLimit.HasValue && Length > LengthLimit.GetValueOrDefault())
            {
                throw new InvalidDataException($"Multipart body length limit {LengthLimit.GetValueOrDefault()} exceeded.");
            }
        }

        private int CopySequenceToBuffer(ReadOnlySequence<byte> sequence, byte[] buffer, int offset, int count)
        {
            var span = buffer.AsSpan(offset);
            count = Math.Min(count, buffer.Length - offset);
            if (sequence.Length > count)
            {
                sequence.Slice(0, count).CopyTo(span);
                return span.Length;
            }

            sequence.CopyTo(span);
            return (int)sequence.Length;
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
                        else
                        {
                            //no match - mark previous matches as read, the current one might be the begining of a new boundary
                            read += i;
                            sequence = sequence.Slice(sequenceReader.Consumed - 1);
                            return (false, read);
                        }
                    }
                    else
                    {
                        //end of sequence
                        //There still might be a partial Match, we need to read more to know for sure
                        _partialMatchIndex = i;
                        sequence = sequence.Slice(read);
                        //also slice partial read to insure _bufferedData is empty in the next check
                        sequence = sequence.Slice(i);
                        return (false, read);
                    }

                }

                //boundary was found
                sequence = sequence.Slice(sequenceReader.Position);
                RawLength += _boundary.BoundaryBytes.Length;
                return (true, read);
            }
            return (false, read);
        }

        private (bool didReachEnd, int readCount) TryCopyToEnd(ref ReadOnlySequence<byte> sequence, byte[] buffer, int offset, int count, bool isFinalBlock)
        {
            var sequenceReader = new SequenceReader<byte>(sequence);
            int read = 0;

            if (isFinalBlock && sequence.Length < _boundary.FinalBoundaryLength - _partialMatchIndex)
            {
                throw new IOException("Unexpected end of Stream, the content may have already been read by another component. ");
            }

            while (!sequenceReader.End)
            {
                if (!sequenceReader.TryReadTo(out ReadOnlySequence<byte> body, _boundary.BoundaryBytes[_partialMatchIndex]))
                {
                    //advance reader to end
                    body = sequence.Slice(sequenceReader.Position);
                    read += CopySequenceToBuffer(body, buffer, offset + read, count - read);
                    sequence = sequence.Slice(read);
                    return (false, read);
                }
                if (_partialMatchIndex != 0 && body.Length > 0)
                {
                    // there was a potential end, but it isn't - Copy Previous Matches to buffer and continue
                    for (int i = offset, j = 0; i < buffer.Length && j < _partialMatchIndex; i++)
                    {
                        buffer[i] = _boundary.BoundaryBytes[j];
                        j++;
                    }
                    read = _partialMatchIndex;
                    read += CopySequenceToBuffer(body, buffer, offset + read, count - read);
                    sequence = sequence.Slice(read);
                    return (false, read);
                }
                if (body.Length > 0)
                {
                    //copy all bytes until persumed boundary
                    read += CopySequenceToBuffer(body, buffer, offset + read, count - read);
                }

                //there are more bytes than the received buffer, so return and slice bufferedData.
                if (read == buffer.Length - offset)
                {
                    sequence = sequence.Slice(read);
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
                        else
                        {
                            //no match - copy previous potential matches
                            int j = 0;
                            for (int bufferIndex = offset + read; bufferIndex < buffer.Length && j < i; bufferIndex++)
                            {
                                buffer[bufferIndex] = _boundary.BoundaryBytes[j];
                                j++;
                            }
                            read += j;
                            _partialMatchIndex = 0;
                            sequence = sequence.Slice(sequenceReader.Consumed - 1);
                            return (false, read);
                        }
                    }
                    else
                    {
                        if (sequenceReader.End)
                        {
                            //might be a partial Match, need to read more to know for sure
                            _partialMatchIndex = i;
                            sequence = sequence.Slice(read);
                            //slice partial read to insure _bufferedData is empty in the next check
                            sequence = sequence.Slice(i);
                            return (false, read);
                        }
                    }
                }
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
                else
                {
                    sequence = sequence.Slice(sequenceReader.Position);
                    RawLength += sequenceReader.Consumed;
                    return true;
                }
            }
            sequence = sequence.Slice(sequenceReader.Position);
            RawLength += sequenceReader.Consumed;
            return false;
        }
    }
}

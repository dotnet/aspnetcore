using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebUtilities
{
    class MultipartPipeReaderStream : Stream
    {
        private readonly PipeReader _pipeReader;
        private long _observedLength = 0;
        private bool _finished = false;
        private bool _metadataSkipped = false;
        private readonly MultipartBoundary _boundary;
        private int _partialMatchIndex = 0;

        private static byte[] CrlfDelimiter = new byte[] { (byte)'\r', (byte)'\n' };
        private static byte[] EndOfFileDelimiter = new byte[] { (byte)'-', (byte)'-' };


        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _observedLength;
        public long RawLength { get; private set; } = 0;

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public MultipartPipeReaderStream(PipeReader pipeReader, MultipartBoundary boundary)
        {
            _pipeReader = pipeReader;
            _boundary = boundary;
        }


        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
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
                        var (didReachEnd, copied) = TryCopyToEnd(ref sequence, buffer, offset, readResult.IsCompleted);
                        _pipeReader.AdvanceTo(sequence.Start);
                        consumed += copied;
                        if (didReachEnd)
                        {
                            _finished = true;
                            _observedLength += copied;
                            RawLength += copied;
                        }

                        if (copied > buffer.Length - offset)
                        {
                            _observedLength += copied;
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

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
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
                        var (didReachEnd, copied) = TryCopyToEnd(ref sequence, buffer, offset, readResult.IsCompleted);
                        _pipeReader.AdvanceTo(sequence.Start);
                        consumed += copied;
                        if (didReachEnd)
                        {
                            _finished = true;
                            _observedLength += copied;
                            RawLength += copied;
                        }

                        if (copied > buffer.Length - offset)
                        {
                            _observedLength += copied;
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

                readResult = await _pipeReader.ReadAsync();
                if (readResult.IsCompleted)
                {
                    _finished = true;
                    _metadataSkipped = true;
                    //handle last result
                    return consumed;
                }

                sequence = readResult.Buffer;
            }
        }

        private int CopySequenceToBuffer(ReadOnlySequence<byte> sequence, byte[] buffer, int offset)
        {
            var span = buffer.AsSpan(offset);
            sequence.CopyTo(span);
            if (sequence.Length > buffer.Length - offset)
            {
                return span.Length;
            }
            return (int)sequence.Length;
        }

        private (bool didReachEnd, int readCount) TryCopyToEnd(ref ReadOnlySequence<byte> sequence, byte[] buffer, int offset, bool isFinalBlock)
        {
            var sequenceReader = new SequenceReader<byte>(sequence);
            int read = 0;
            while (!sequenceReader.End)
            {
                if (sequenceReader.TryReadTo(out ReadOnlySequence<byte> body, _boundary.BoundaryBytes[_partialMatchIndex]))
                {
                    if (_partialMatchIndex != 0 && body.Length > 0)
                    {
                        // there was a potential end, but it isn't - Copy Previous Matches to buffer and continue
                        for (int i = offset, j = 0; i < buffer.Length && j < _partialMatchIndex; i++)
                        {
                            buffer[i] = _boundary.BoundaryBytes[j];
                            j++;
                        }
                        read = _partialMatchIndex;
                        read += CopySequenceToBuffer(body, buffer, offset + read);
                        sequence = sequence.Slice(read);
                        return (false, read);
                    }
                    if (body.Length > 0)
                    {
                        //copy all bytes until persumed boundary
                        read += CopySequenceToBuffer(body, buffer, offset + read);
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
                    sequence = sequence.Slice(_boundary.BoundaryBytes.Length - _partialMatchIndex);
                    RawLength += _boundary.BoundaryBytes.Length;
                    return (true, read);
                }
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
                        continue;
                    }
                }

                var position = sequenceReader.Position;
                sequenceReader.Rewind(sequenceReader.Consumed);
                if (sequenceReader.TryReadTo(out var _, EndOfFileDelimiter))
                {
                    //Debug.Assert(sequenceReader.Consumed < consumed, "Un-expected data found on the boundary line: " + body);
                    sequence = sequence.Slice(position);
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

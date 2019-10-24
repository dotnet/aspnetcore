using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.WebUtilities
{
    public class MultipartPipeReader
    {

        public const int DefaultHeadersCountLimit = 16;
        public const int DefaultHeadersLengthLimit = 1024 * 16;
        //private const int DefaultBufferSize = 1024 * 4;
        private const int StackAllocThreshold = 128;

        private readonly PipeReader _pipeReader;
        //private ReadOnlySequence<byte> _buffer;
        private long _bytesConsumed = 0;
        private readonly MultipartBoundary _boundary;
        private MultipartPipeReaderStream _currentStream;

        private static ReadOnlySpan<byte> ColonDelimiter => new byte[] { (byte)':' };
        private static ReadOnlySpan<byte> CrlfDelimiter => new byte[] { (byte)'\r', (byte)'\n' };


        public MultipartPipeReader(string boundary, PipeReader pipeReader)
        {
            _pipeReader = pipeReader ?? throw new ArgumentNullException(nameof(pipeReader));
            _boundary = new MultipartBoundary(boundary ?? throw new ArgumentNullException(nameof(boundary)), false);

            // This stream will drain any preamble data and remove the first boundary marker. 
            // TODO: HeadersLengthLimit can't be modified until after the constructor. 
            _currentStream = new MultipartPipeReaderStream(_pipeReader, _boundary);
        }

        /// <summary>
        /// The limit for the number of headers to read.
        /// </summary>
        public int HeadersCountLimit { get; set; } = DefaultHeadersCountLimit;

        /// <summary>
        /// The combined size limit for headers per multipart section.
        /// </summary>
        public int HeadersLengthLimit { get; set; } = DefaultHeadersLengthLimit;

        /// <summary>
        /// The optional limit for the total response body length.
        /// </summary>
        public long? BodyLengthLimit { get; set; }


        public async Task<MultipartSection> ReadNextSectionAsync(CancellationToken cancellationToken = default)
        {
            await _currentStream.DrainAsync(cancellationToken);
            _bytesConsumed += _currentStream.Length;


            var headersAccumulator = new KeyValueAccumulator();
            ReadOnlySequence<byte> buffer = default;
            ReadResult readResult = default;
            _boundary.ExpectLeadingCrlf = true;
            while (true)
            {
                if (buffer.IsEmpty)
                {
                    readResult = await _pipeReader.ReadAsync(cancellationToken);
                    buffer = readResult.Buffer;
                }

                if (!buffer.IsEmpty)
                {
                    if (TryParseHeaders(ref buffer, ref headersAccumulator, readResult.IsCompleted))
                    {
                        _pipeReader.AdvanceTo(buffer.Start);
                        _currentStream = new MultipartPipeReaderStream(_pipeReader, _boundary);
                        return new MultipartSection() { Headers = headersAccumulator.GetResults(), Body = _currentStream, BaseStreamOffset = _bytesConsumed }; ;
                    }
                }

                if (readResult.IsCompleted)
                {
                    _pipeReader.AdvanceTo(buffer.End);

                    if (!buffer.IsEmpty)
                    {
                        throw new InvalidOperationException("End of body before form was fully parsed.");
                    }
                    return null;
                }
            }

        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal bool TryParseHeaders(
            ref ReadOnlySequence<byte> buffer,
            ref KeyValueAccumulator accumulator,
            bool isFinalBlock)
        {
            if (buffer.IsSingleSegment)
            {
                var didFinishParsing = ParseHeadersFast(buffer.First.Span,
                    ref accumulator,
                    isFinalBlock,
                    out var consumed);

                if (HeadersLengthLimit < consumed)
                {
                    throw new InvalidDataException($"Multipart headers length limit {HeadersLengthLimit} exceeded.");
                }

                buffer = buffer.Slice(consumed);
                _bytesConsumed += consumed;
                return didFinishParsing;
            }

            return ParseHeadersSlow(ref buffer,
                ref accumulator,
                isFinalBlock);
        }

        // Fast parsing for single span in ReadOnlySequence
        private bool ParseHeadersFast(ReadOnlySpan<byte> span,
            ref KeyValueAccumulator accumulator,
            bool isFinalBlock,
            out int consumed)
        {
            ReadOnlySpan<byte> key;
            ReadOnlySpan<byte> value;
            consumed = 0;

            while (span.Length > 0)
            {
                // Find the end of the header.
                var newLine = span.IndexOf(CrlfDelimiter);
                ReadOnlySpan<byte> line;
                int colon;
                var foundNewLine = newLine != -1;

                if (foundNewLine)
                {
                    line = span.Slice(0, newLine);
                    span = span.Slice(line.Length + CrlfDelimiter.Length);
                    consumed += line.Length + CrlfDelimiter.Length;
                }
                else
                {
                    // We can't know that what is currently read is the end of the header value, that's only the case if this is the final block
                    // If we're not in the final block, then consume nothing
                    if (!isFinalBlock)
                    {
                        // Don't buffer indefinately
                        if (span.Length > DefaultHeadersLengthLimit)
                        {
                            throw new InvalidDataException($"Multipart headers length limit {HeadersLengthLimit} exceeded.");
                        }
                        return false;
                    }

                    line = span;
                    span = default;
                    consumed += line.Length;
                }

                if (line.Length == 0) // an empty line means it's the end of the headers
                {
                    return true;
                }

                colon = line.IndexOf(ColonDelimiter);

                if (colon == -1)
                {
                    throw new InvalidDataException($"Invalid header line: {line.ToString()}");
                }
                else
                {
                    if (line.Length > HeadersLengthLimit)
                    {
                        throw new InvalidDataException($"Multipart headers length limit {HeadersLengthLimit} exceeded.");
                    }
                    key = line.Slice(0, colon);
                    value = line.Slice(colon + ColonDelimiter.Length);
                }

                var decodedKey = GetDecodedString(key);
                var decodedValue = GetDecodedString(value).Trim();

                AppendAndVerify(ref accumulator, decodedKey, decodedValue);
            }
            return false;
        }

        // For multi-segment parsing of a read only sequence
        private bool ParseHeadersSlow(
            ref ReadOnlySequence<byte> buffer,
            ref KeyValueAccumulator accumulator,
            bool isFinalBlock)
        {
            var sequenceReader = new SequenceReader<byte>(buffer);

            var position = sequenceReader.Position;
            var consumedBytes = default(long);

            while (!sequenceReader.End)
            {
                if (!sequenceReader.TryReadTo(out ReadOnlySequence<byte> line, CrlfDelimiter))
                {
                    if (!isFinalBlock)
                    {
                        // Don't buffer indefinately
                        if ((sequenceReader.Consumed - consumedBytes) > DefaultHeadersLengthLimit)
                        {
                            throw new InvalidDataException($"Multipart headers length limit {HeadersLengthLimit} exceeded.");
                        }
                        break;
                    }

                    // This must be the final key=value pair
                    line = buffer.Slice(sequenceReader.Position);
                    _bytesConsumed += consumedBytes+sequenceReader.Consumed;
                    sequenceReader.Advance(line.Length);
                }

                if (consumedBytes > DefaultHeadersLengthLimit)
                {
                    throw new InvalidDataException($"Multipart headers length limit {HeadersLengthLimit} exceeded.");
                }

                if (line.IsSingleSegment)
                {
                    var didFinishParsing = ParseHeadersFast(line.FirstSpan, ref accumulator, isFinalBlock: true, out var segmentConsumed);
                    Debug.Assert(segmentConsumed == line.FirstSpan.Length);
                    consumedBytes = sequenceReader.Consumed;
                    position = sequenceReader.Position;
                    if (didFinishParsing)
                    {
                        buffer = buffer.Slice(position);
                        _bytesConsumed += consumedBytes;
                        return true;
                    }
                    continue;
                }

                var lineReader = new SequenceReader<byte>(line);
                ReadOnlySequence<byte> value;

                if (lineReader.Length == 0)
                {
                    buffer = buffer.Slice(position);
                    _bytesConsumed += consumedBytes;
                    return true;
                }

                if (lineReader.TryReadTo(out var key, ColonDelimiter))
                {
                    value = line.Slice(lineReader.Position);
                }
                else
                {
                    throw new InvalidDataException($"Invalid header line: {line.ToString()}");
                }

                var decodedKey = GetDecodedStringFromReadOnlySequence(key);
                var decodedValue = GetDecodedStringFromReadOnlySequence(value);

                AppendAndVerify(ref accumulator, decodedKey, decodedValue);

                consumedBytes = sequenceReader.Consumed;
                position = sequenceReader.Position;
            }

            buffer = buffer.Slice(position);
            _bytesConsumed += consumedBytes;
            return false;
        }


        // Check that key/value constraints are met and appends value to accumulator.
        private void AppendAndVerify(ref KeyValueAccumulator accumulator, string decodedKey, string decodedValue)
        {
            accumulator.Append(decodedKey, decodedValue);

            if (accumulator.KeyCount > HeadersCountLimit)
            {
                throw new InvalidDataException($"Multipart headers count limit {HeadersCountLimit} exceeded.");
            }
        }


        private string GetDecodedStringFromReadOnlySequence(in ReadOnlySequence<byte> ros)
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

        private string GetDecodedString(ReadOnlySpan<byte> readOnlySpan)
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

                var bytes = UrlDecoder.DecodeInPlace(span, isFormEncoding: true);
                span = span.Slice(0, bytes);

                return Encoding.UTF8.GetString(span);
            }
        }
    }
}

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
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities
{
    class MultipartPipeReader
    {

        public const int DefaultHeadersCountLimit = 16;
        public const int DefaultHeadersLengthLimit = 1024 * 16;
        private const int DefaultBufferSize = 1024 * 4;
        private const int StackAllocThreshold = 128;

        private readonly PipeReader _pipeReader;
        private ReadOnlySequence<byte> _buffer;
        private ReadResult _readResult = default;
        private readonly BufferedReadStream _stream;
        private readonly MultipartBoundary _boundary;
        private MultipartReaderStream _currentStream;


        public MultipartPipeReader(string boundary, PipeReader pipeReader)
        {
            _pipeReader = pipeReader ?? throw new ArgumentNullException(nameof(pipeReader));
            _boundary = new MultipartBoundary(boundary ?? throw new ArgumentNullException(nameof(boundary)), false);
            _buffer = default;

            _stream = new BufferedReadStream(pipeReader.AsStream(), DefaultBufferSize);
            _currentStream = new MultipartReaderStream(_stream, _boundary) { LengthLimit = HeadersLengthLimit };

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


        public async Task<MultipartSection> ReadNextSectionAsync(CancellationToken cancellationToken = new CancellationToken())
        {

            if (_buffer.IsEmpty)
            {
                _readResult = await _pipeReader.ReadAsync(cancellationToken);
                _buffer = _readResult.Buffer;
            }

            SkipMetaData(ref _buffer, _readResult.IsCompleted);
            if(_buffer.IsEmpty)
            {
                return null;
            }
            var headersAccumulator = new KeyValueAccumulator();
            ParseHeaders(ref _buffer, ref headersAccumulator, _readResult.IsCompleted);
            _boundary.ExpectLeadingCrlf = true;
            var body = ReadBody(ref _buffer, _readResult.IsCompleted);
            return new MultipartSection() { Headers = headersAccumulator.GetResults(), Body = new MemoryStream(body), BaseStreamOffset = 0 };
        }


        void SkipMetaData(ref ReadOnlySequence<byte> buffer, bool isFinalBlock)
        {
            var sequenceReader = new SequenceReader<byte>(buffer);
            var consumedBytes = default(long);
            var crlfDelimiter = new byte[] { (byte)'\r', (byte)'\n' };
            var endOfFileDelimiter = new byte[] { (byte)'-', (byte)'-' };

            if (!sequenceReader.TryReadTo(out var body, crlfDelimiter))
            {
                if (sequenceReader.TryReadTo(out var _, endOfFileDelimiter))
                {
                    buffer = buffer.Slice(sequenceReader.Position);
                    return;
                }

                if (!isFinalBlock)
                {
                    // Don't buffer indefinately
                    if ((sequenceReader.Consumed - consumedBytes) > 100)
                    {
                        Debug.Print("Un-expected data found on the boundary line: " + body);
                        return;
                    }
                }
            }
            var position = sequenceReader.Position;
            var consumed = sequenceReader.Consumed;
            sequenceReader.Rewind(sequenceReader.Consumed);
            if (sequenceReader.TryReadTo(out var _, endOfFileDelimiter))
            {
                if (sequenceReader.Consumed < consumed)
                {
                    buffer = buffer.Slice(sequenceReader.Position);
                }
            }
            buffer = buffer.Slice(position);

        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void ParseHeaders(
            ref ReadOnlySequence<byte> buffer,
            ref KeyValueAccumulator accumulator,
            bool isFinalBlock)
        {
            if (buffer.IsSingleSegment)
            {
                ParseHeadersFast(buffer.First.Span,
                    ref accumulator,
                    isFinalBlock,
                    out var consumed);

                buffer = buffer.Slice(consumed);
                return;
            }

            ParseValuesSlow(ref buffer,
                ref accumulator,
                isFinalBlock);
        }

        // Fast parsing for single span in ReadOnlySequence
        private void ParseHeadersFast(ReadOnlySpan<byte> span,
            ref KeyValueAccumulator accumulator,
            bool isFinalBlock,
            out int consumed)
        {
            ReadOnlySpan<byte> key;
            ReadOnlySpan<byte> value;
            consumed = 0;
            var colonDelimiter = new byte[] { (byte)':' };
            var crlfDelimiter = new byte[] { (byte)'\r', (byte)'\n' };

            while (span.Length > 0)
            {
                // Find the end of the header.
                var newLine = span.IndexOf(crlfDelimiter);
                ReadOnlySpan<byte> line;
                int colon;
                var foundNewLine = newLine != -1;

                if (foundNewLine)
                {
                    line = span.Slice(0, newLine);
                    span = span.Slice(line.Length + crlfDelimiter.Length);
                    consumed += line.Length + crlfDelimiter.Length;
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
                        return;
                    }

                    line = span;
                    span = default;
                    consumed += line.Length;
                }

                if (line.Length == 0) // an empty line means it's the end of the headers
                {
                    break;
                }

                colon = line.IndexOf(colonDelimiter);

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
                    value = line.Slice(colon + colonDelimiter.Length);
                }

                var decodedKey = GetDecodedString(key);
                var decodedValue = GetDecodedString(value).Trim();

                AppendAndVerify(ref accumulator, decodedKey, decodedValue);
            }
        }

        private byte[] ReadBody(ref ReadOnlySequence<byte> buffer, bool isFinalBlock)
        {
            var sequenceReader = new SequenceReader<byte>(buffer);
            ReadOnlySequence<byte> body;

            var consumed = sequenceReader.Position;
            var consumedBytes = default(long);

            if (!sequenceReader.TryReadTo(out body, _boundary.BoundaryBytes))
            {
                if (!isFinalBlock)
                {
                    // Don't buffer indefinately
                    if ((sequenceReader.Consumed - consumedBytes) > BodyLengthLimit)
                    {
                        throw new InvalidDataException($"Multipart body length limit {BodyLengthLimit} exceeded.");
                    }
                }

                //// This must be the final key=value pair
                //body = buffer.Slice(sequenceReader.Position);
                //sequenceReader.Advance(body.Length);
            }
            buffer = buffer.Slice(sequenceReader.Position);
            return GetBytesFromReadOnlySequence(body);
        }


        // For multi-segment parsing of a read only sequence
        private void ParseValuesSlow(
            ref ReadOnlySequence<byte> buffer,
            ref KeyValueAccumulator accumulator,
            bool isFinalBlock)
        {
            var sequenceReader = new SequenceReader<byte>(buffer);
            ReadOnlySequence<byte> line;

            var consumed = sequenceReader.Position;
            var consumedBytes = default(long);
            var colonDelimiter = new byte[] { (byte)':' };
            var crlfDelimiter = new byte[] { (byte)'\r', (byte)'\n' };

            while (!sequenceReader.End)
            {
                if (!sequenceReader.TryReadTo(out line, crlfDelimiter))
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
                    sequenceReader.Advance(line.Length);
                }

                if (line.IsSingleSegment)
                {
                    ParseHeadersFast(line.FirstSpan, ref accumulator, isFinalBlock: true, out var segmentConsumed);
                    Debug.Assert(segmentConsumed == line.FirstSpan.Length);
                    consumedBytes = sequenceReader.Consumed;
                    consumed = sequenceReader.Position;
                    continue;
                }

                var lineReader = new SequenceReader<byte>(line);
                ReadOnlySequence<byte> value;

                if (lineReader.Length == 0)
                {
                    break;
                }

                if (lineReader.Length > HeadersLengthLimit)
                {
                    throw new InvalidDataException($"Multipart headers length limit {HeadersLengthLimit} exceeded.");
                }

                if (lineReader.TryReadTo(out var key, colonDelimiter))
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
                consumed = sequenceReader.Position;
            }

            buffer = buffer.Slice(consumed);
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

        private byte[] GetBytesFromReadOnlySequence(in ReadOnlySequence<byte> ros)
        {
            if (ros.IsSingleSegment)
            {
                return GetBytes(ros.First.Span);
            }

            if (ros.Length < StackAllocThreshold)
            {
                Span<byte> buffer = stackalloc byte[(int)ros.Length];
                ros.CopyTo(buffer);
                return GetBytes(buffer);
            }
            else
            {
                var byteArray = ArrayPool<byte>.Shared.Rent((int)ros.Length);

                try
                {
                    Span<byte> buffer = byteArray.AsSpan(0, (int)ros.Length);
                    ros.CopyTo(buffer);
                    return GetBytes(buffer);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(byteArray);
                }
            }
        }

        private byte[] GetBytes(ReadOnlySpan<byte> readOnlySpan)
        {
            if (readOnlySpan.Length == 0)
            {
                return new byte[] { };
            }
            else
            {
                // We need to create a Span from a ReadOnlySpan. This cast is safe because the memory is still held by the pipe
                // We will also create a string from it by the end of the function.
                var span = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(readOnlySpan[0]), readOnlySpan.Length);

                var bytes = UrlDecoder.DecodeInPlace(span, isFormEncoding: true);
                span = span.Slice(0, bytes);

                return span.ToArray();
            }
        }
    }
}

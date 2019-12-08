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
        private const int StackAllocThreshold = 128;

        private readonly PipeReader _pipeReader;
        private long _bytesConsumed = 0;
        private readonly MultipartBoundary _boundary;
        private MultipartSectionPipeReader _currentSection;
        private readonly bool _trackBaseOffsets;

        private static ReadOnlySpan<byte> ColonDelimiter => new byte[] { (byte)':' };
        private static ReadOnlySpan<byte> CrlfDelimiter => new byte[] { (byte)'\r', (byte)'\n' };

        public MultipartPipeReader(string boundary, PipeReader pipeReader, bool trackBaseOffsets)
        {
            _pipeReader = pipeReader ?? throw new ArgumentNullException(nameof(pipeReader));
            _boundary = new MultipartBoundary(boundary ?? throw new ArgumentNullException(nameof(boundary)), false);

            // This stream will drain any preamble data and remove the first boundary marker. 
            // TODO: HeadersLengthLimit can't be modified until after the constructor. 
            _currentSection = new MultipartSectionPipeReader(_pipeReader, _boundary, trackBaseOffsets);
            _trackBaseOffsets = trackBaseOffsets;
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
            await _currentSection.DrainAsync(cancellationToken);
            _bytesConsumed += _currentSection.RawLength;


            var headersAccumulator = new KeyValueAccumulator();
            _boundary.ExpectLeadingCrlf = true;
            long headersLength = 0;
            while (true)
            {
                var readResult = await _pipeReader.ReadAsync(cancellationToken);
                var buffer = readResult.Buffer;

                if (!buffer.IsEmpty)
                {
                    var finishedParsing = TryParseHeadersToEnd(ref buffer, ref headersAccumulator, ref headersLength);
                    if (headersLength > DefaultHeadersLengthLimit)
                    {
                        throw new InvalidDataException($"Multipart headers length limit {HeadersLengthLimit} exceeded.");
                    }
                    if (finishedParsing)
                    {
                        _bytesConsumed += headersLength;
                        _pipeReader.AdvanceTo(buffer.Start);
                        _currentSection = new MultipartSectionPipeReader(_pipeReader, _boundary, _trackBaseOffsets);
                        long? baseStreamOffset = _trackBaseOffsets ? (long?)_bytesConsumed : null;
                        return new MultipartSection() { Headers = headersAccumulator.GetResults(), BodyReader = _currentSection, BaseStreamOffset = baseStreamOffset }; ;
                    }
                    if (readResult.IsCompleted)
                    {
                        throw new InvalidDataException("Unexpected end of Stream, the content may have already been read by another component. ");
                    }

                    _pipeReader.AdvanceTo(buffer.Start, buffer.End);
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
        internal bool TryParseHeadersToEnd(
            ref ReadOnlySequence<byte> buffer,
            ref KeyValueAccumulator accumulator,
            ref long headersLength)
        {
            if (buffer.IsSingleSegment)
            {
                var didFinishParsing = TryParseHeadersToEndFast(buffer.First.Span,
                    ref accumulator,
                    HeadersLengthLimit - headersLength,
                    out var consumed);
                headersLength += consumed;
                buffer = buffer.Slice(consumed);
                return didFinishParsing;
            }

            return TryParseHeadersToEndSlow(ref buffer,
                ref accumulator,
                ref headersLength);
        }

        // Fast parsing for single span in ReadOnlySequence
        private bool TryParseHeadersToEndFast(ReadOnlySpan<byte> span,
            ref KeyValueAccumulator accumulator,
            long lengthLimit,
            out int consumed)
        {
            consumed = 0;

            while (span.Length > 0)
            {
                // Find the end of the header.
                var newLine = span.IndexOf(CrlfDelimiter);
                ReadOnlySpan<byte> line;
                var foundNewLine = newLine != -1;

                if (foundNewLine)
                {
                    line = span.Slice(0, newLine);
                    span = span.Slice(line.Length + CrlfDelimiter.Length);
                    consumed += line.Length + CrlfDelimiter.Length;
                }
                // We can't know that what is currently read is the end of the header value, that's only the case if this is the final block
                // If we're not in the final block, then consume nothing
                else
                {
                    // Don't buffer indefinately
                    if (span.Length > lengthLimit)
                    {
                        throw new InvalidDataException($"Line length limit {lengthLimit} exceeded.");
                    }
                    return false;
                }

                if (line.Length == 0) // an empty line means it's the end of the headers
                {
                    return true;
                }

                if (line.Length > lengthLimit)
                {
                    throw new InvalidDataException($"Line length limit {lengthLimit} exceeded.");
                }

                ParseHeaderLineFast(line, ref accumulator);
                lengthLimit -= line.Length;
            }
            return false;
        }


        private void ParseHeaderLineFast(ReadOnlySpan<byte> line,
          ref KeyValueAccumulator accumulator)
        {
            ReadOnlySpan<byte> key;
            ReadOnlySpan<byte> value;

            int colon = line.IndexOf(ColonDelimiter);

            if (colon == -1)
            {
                throw new InvalidDataException($"Invalid header line: {GetTrimmedString(line)}");
            }

            key = line.Slice(0, colon);
            value = line.Slice(colon + ColonDelimiter.Length);

            var decodedKey = GetTrimmedString(key);
            var decodedValue = GetTrimmedString(value);
            AppendAndVerify(ref accumulator, decodedKey, decodedValue);
        }

        // For multi-segment parsing of a read only sequence
        private bool TryParseHeadersToEndSlow(
            ref ReadOnlySequence<byte> buffer,
            ref KeyValueAccumulator accumulator,
            ref long headersLength)
        {
            var sequenceReader = new SequenceReader<byte>(buffer);

            while (!sequenceReader.End)
            {
                if (!sequenceReader.TryReadTo(out ReadOnlySequence<byte> line, CrlfDelimiter))
                {
                    // Don't buffer indefinately
                    if (headersLength + sequenceReader.Consumed > HeadersLengthLimit)
                    {
                        throw new InvalidDataException($"Line length limit {HeadersLengthLimit - headersLength} exceeded.");
                    }

                    return false;
                }

                if (sequenceReader.Consumed + headersLength > HeadersLengthLimit)
                {
                    throw new InvalidDataException($"Multipart headers length limit {HeadersLengthLimit} exceeded.");
                }

                if (line.IsEmpty)
                {
                    buffer = buffer.Slice(sequenceReader.Position);
                    headersLength += sequenceReader.Consumed;
                    return true;
                }

                if (line.IsSingleSegment)
                {
                    ParseHeaderLineFast(line.FirstSpan, ref accumulator);
                    continue;
                }

                var lineReader = new SequenceReader<byte>(line);
                ReadOnlySequence<byte> value;

                if (!lineReader.TryReadTo(out var key, ColonDelimiter))
                {
                    throw new InvalidDataException($"Invalid header line: {GetStringFromReadOnlySequence(line)}");
                }
                value = line.Slice(lineReader.Position);


                var decodedKey = GetStringFromReadOnlySequence(key);
                var decodedValue = GetStringFromReadOnlySequence(value);

                AppendAndVerify(ref accumulator, decodedKey, decodedValue);
            }

            buffer = default;
            headersLength += sequenceReader.Consumed;
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

        private string GetStringFromReadOnlySequence(in ReadOnlySequence<byte> ros)
        {
            if (ros.IsSingleSegment)
            {
                return GetTrimmedString(ros.First.Span);
            }

            if (ros.Length < StackAllocThreshold)
            {
                Span<byte> buffer = stackalloc byte[(int)ros.Length];
                ros.CopyTo(buffer);
                return GetTrimmedString(buffer);
            }

            var byteArray = ArrayPool<byte>.Shared.Rent((int)ros.Length);

            try
            {
                Span<byte> buffer = byteArray.AsSpan(0, (int)ros.Length);
                ros.CopyTo(buffer);
                return GetTrimmedString(buffer);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteArray);
            }

        }

        private string GetTrimmedString(ReadOnlySpan<byte> readOnlySpan)
        {
            if (readOnlySpan.Length == 0)
            {
                return string.Empty;
            }

            // We need to create a Span from a ReadOnlySpan. This cast is safe because the memory is still held by the pipe
            // We will also create a string from it by the end of the function.
            var span = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(readOnlySpan[0]), readOnlySpan.Length);
            return Encoding.UTF8.GetString(span.Trim((byte)' '));
        }
    }
}

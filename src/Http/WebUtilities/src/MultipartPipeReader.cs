// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities
{
    /// <summary>
    /// Pipe implementation of a reader for HTTP Multipart Content Type
    /// </summary>
    public class MultipartPipeReader
    {
        private const int StackAllocThreshold = 128;

        private readonly PipeReader _pipeReader;
        private readonly MultipartBoundary _boundary;
        private MultipartSectionPipeReader? _currentSectionReader;

        private static ReadOnlySpan<byte> ColonDelimiter => new byte[] { (byte)':' };
        private static ReadOnlySpan<byte> CrlfDelimiter => new byte[] { (byte)'\r', (byte)'\n' };

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="boundary">the Multipart boundary</param>
        /// <param name="pipeReader">a Pipe Reader</param>
        public MultipartPipeReader(string boundary, PipeReader pipeReader)
        {
            _pipeReader = pipeReader ?? throw new ArgumentNullException(nameof(pipeReader));
            _boundary = new MultipartBoundary(boundary ?? throw new ArgumentNullException(nameof(boundary)), false);
        }

        /// <summary>
        /// The limit for the number of headers to read.
        /// </summary>
        public int HeadersCountLimit { get; set; } = MultipartReader.DefaultHeadersCountLimit;

        /// <summary>
        /// The combined size limit for headers per multipart section.
        /// </summary>
        public int HeadersLengthLimit { get; set; } = MultipartReader.DefaultHeadersLengthLimit;

        /// <summary>
        /// The optional size limit for each section body.
        /// </summary>
        public long? BodyLengthLimit { get; set; }

        /// <summary>
        /// Read the next Multipart Section
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async Task<MultipartPipeSection?> ReadNextSectionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentSectionReader == null) // first call.
            {
                // This section will drain any preamble data and remove the first boundary marker.
                _currentSectionReader = new MultipartSectionPipeReader(_pipeReader, _boundary) { LengthLimit = HeadersLengthLimit };
            }

            await _currentSectionReader.DrainAsync(cancellationToken);

            if (_currentSectionReader.FinalBoundaryFound)
            {
                return null;
            }

            var headers = await ReadHeadersAsync(cancellationToken);
            _boundary.ExpectLeadingCrlf = true;
            _currentSectionReader = new MultipartSectionPipeReader(_pipeReader, _boundary)
            {
                LengthLimit = BodyLengthLimit,
            };
            return new MultipartPipeSection()
            {
                Headers = headers,
                BodyReader = _currentSectionReader,
            };
        }

        internal async Task<Dictionary<string, StringValues>> ReadHeadersAsync(CancellationToken cancellationToken)
        {
            var headersAccumulator = new KeyValueAccumulator();
            long headersLength = 0;
            while (true)
            {
                var readResult = await _pipeReader.ReadAsync(cancellationToken);

                if (readResult.IsCanceled)
                {
                    throw new OperationCanceledException("Read was canceled");
                }

                var buffer = readResult.Buffer;
                try
                {
                    var finishedParsing = TryParseHeadersToEnd(ref buffer, ref headersAccumulator, ref headersLength);

                    if (finishedParsing)
                    {
                        return headersAccumulator.GetResults();
                    }

                    if (readResult.IsCompleted)
                    {
                        throw new InvalidDataException("Unexpected end of Stream, could not read all multipart headers.");
                    }
                }
                finally
                {
                    _pipeReader.AdvanceTo(buffer.Start, buffer.End);
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
                    // Don't buffer indefinitely
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
                    // Don't buffer indefinitely
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

                if (!lineReader.TryReadTo(out ReadOnlySequence<byte> key, ColonDelimiter))
                {
                    throw new InvalidDataException($"Invalid header line: {GetStringFromReadOnlySequence(line)}");
                }
                var value = line.Slice(lineReader.Position);

                var decodedKey = GetStringFromReadOnlySequence(key);
                var decodedValue = GetStringFromReadOnlySequence(value);

                AppendAndVerify(ref accumulator, decodedKey, decodedValue);
            }

            buffer = buffer.Slice(sequenceReader.Position);
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

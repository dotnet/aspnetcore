// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities
{
    // https://www.ietf.org/rfc/rfc2046.txt
    public class MultipartReader
    {
        public const int DefaultHeadersCountLimit = 16;
        public const int DefaultHeadersLengthLimit = 1024 * 16;
        private const int DefaultBufferSize = 1024 * 4;

        private readonly BufferedReadStream _stream;
        private readonly MultipartBoundary _boundary;
        private MultipartReaderStream _currentStream;

        public MultipartReader(string boundary, Stream stream)
            : this(boundary, stream, DefaultBufferSize)
        {
        }

        public MultipartReader(string boundary, Stream stream, int bufferSize)
        {
            if (boundary == null)
            {
                throw new ArgumentNullException(nameof(boundary));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (bufferSize < boundary.Length + 8) // Size of the boundary + leading and trailing CRLF + leading and trailing '--' markers.
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "Insufficient buffer space, the buffer must be larger than the boundary: " + boundary);
            }
            _stream = new BufferedReadStream(stream, bufferSize);
            _boundary = new MultipartBoundary(boundary, false);
            // This stream will drain any preamble data and remove the first boundary marker.
            // TODO: HeadersLengthLimit can't be modified until after the constructor.
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
            // Drain the prior section.
            await _currentStream.DrainAsync(cancellationToken);
            // If we're at the end return null
            if (_currentStream.FinalBoundaryFound)
            {
                // There may be trailer data after the last boundary.
                await _stream.DrainAsync(HeadersLengthLimit, cancellationToken);
                return null;
            }
            var headers = await ReadHeadersAsync(cancellationToken);
            _boundary.ExpectLeadingCrlf = true;
            _currentStream = new MultipartReaderStream(_stream, _boundary) { LengthLimit = BodyLengthLimit };
            long? baseStreamOffset = _stream.CanSeek ? (long?)_stream.Position : null;
            return new MultipartSection() { Headers = headers, Body = _currentStream, BaseStreamOffset = baseStreamOffset };
        }

        private async Task<Dictionary<string, StringValues>> ReadHeadersAsync(CancellationToken cancellationToken)
        {
            int totalSize = 0;
            var accumulator = new KeyValueAccumulator();
            var line = await _stream.ReadLineAsync(HeadersLengthLimit - totalSize, cancellationToken);
            while (!string.IsNullOrEmpty(line))
            {
                if (HeadersLengthLimit - totalSize < line.Length)
                {
                    throw new InvalidDataException($"Multipart headers length limit {HeadersLengthLimit} exceeded.");
                }
                totalSize += line.Length;
                int splitIndex = line.IndexOf(':');
                if (splitIndex <= 0)
                {
                    throw new InvalidDataException($"Invalid header line: {line}");
                }

                var name = line.Substring(0, splitIndex);
                var value = line.Substring(splitIndex + 1, line.Length - splitIndex - 1).Trim();
                accumulator.Append(name, value);
                if (accumulator.KeyCount > HeadersCountLimit)
                {
                    throw new InvalidDataException($"Multipart headers count limit {HeadersCountLimit} exceeded.");
                }

                line = await _stream.ReadLineAsync(HeadersLengthLimit - totalSize, cancellationToken);
            }

            return accumulator.GetResults();
        }
    }
}
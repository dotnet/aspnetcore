// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.WebUtilities
{
    // https://www.ietf.org/rfc/rfc2046.txt
    public class MultipartReader
    {
        private const int DefaultBufferSize = 1024 * 4;

        private readonly BufferedReadStream _stream;
        private readonly string _boundary;
        private MultipartReaderStream _currentStream;

        public MultipartReader([NotNull] string boundary, [NotNull] Stream stream)
            : this(boundary, stream, DefaultBufferSize)
        {
        }

        public MultipartReader([NotNull] string boundary, [NotNull] Stream stream, int bufferSize)
        {
            if (bufferSize < boundary.Length + 8) // Size of the boundary + leading and trailing CRLF + leading and trailing '--' markers.
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "Insufficient buffer space, the buffer must be larger than the boundary: " + boundary);
            }
            _stream = new BufferedReadStream(stream, bufferSize);
            _boundary = boundary;
            // This stream will drain any preamble data and remove the first boundary marker.
            _currentStream = new MultipartReaderStream(_stream, _boundary, expectLeadingCrlf: false);
        }

        /// <summary>
        /// The limit for individual header lines inside a multipart section.
        /// </summary>
        public int HeaderLengthLimit { get; set; } = 1024 * 4;

        /// <summary>
        /// The combined size limit for headers per multipart section.
        /// </summary>
        public int TotalHeaderSizeLimit { get; set; } = 1024 * 16;

        public async Task<MultipartSection> ReadNextSectionAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            // Drain the prior section.
            await _currentStream.DrainAsync(cancellationToken);
            // If we're at the end return null
            if (_currentStream.FinalBoundaryFound)
            {
                // There may be trailer data after the last boundary.
                await _stream.DrainAsync(cancellationToken);
                return null;
            }
            var headers = await ReadHeadersAsync(cancellationToken);
            _currentStream = new MultipartReaderStream(_stream, _boundary);
            long? baseStreamOffset = _stream.CanSeek ? (long?)_stream.Position : null;
            return new MultipartSection() { Headers = headers, Body = _currentStream, BaseStreamOffset = baseStreamOffset };
        }

        private async Task<IDictionary<string, StringValues>> ReadHeadersAsync(CancellationToken cancellationToken)
        {
            int totalSize = 0;
            var accumulator = new KeyValueAccumulator();
            var line = await _stream.ReadLineAsync(HeaderLengthLimit, cancellationToken);
            while (!string.IsNullOrEmpty(line))
            {
                totalSize += line.Length;
                if (totalSize > TotalHeaderSizeLimit)
                {
                    throw new InvalidOperationException("Total header size limit exceeded: " + TotalHeaderSizeLimit);
                }
                int splitIndex = line.IndexOf(':');
                Debug.Assert(splitIndex > 0, "Invalid header line: " + line);
                if (splitIndex >= 0)
                {
                    var name = line.Substring(0, splitIndex);
                    var value = line.Substring(splitIndex + 1, line.Length - splitIndex - 1).Trim();
                    accumulator.Append(name, value);
                }
                line = await _stream.ReadLineAsync(HeaderLengthLimit, cancellationToken);
            }

            return accumulator.GetResults();
        }
    }
}
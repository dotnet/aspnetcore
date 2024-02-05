// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.WebUtilities;

// https://www.ietf.org/rfc/rfc2046.txt
/// <summary>
/// Reads multipart form content from the specified <see cref="Stream"/>.
/// </summary>
public class MultipartReader
{
    /// <summary>
    /// Gets the default value for <see cref="HeadersCountLimit"/>.
    /// Defaults to 16.
    /// </summary>
    public const int DefaultHeadersCountLimit = 16;

    /// <summary>
    /// Gets the default value for <see cref="HeadersLengthLimit"/>.
    /// Defaults to 16,384 bytes, which is approximately 16KB.
    /// </summary>
    public const int DefaultHeadersLengthLimit = 1024 * 16;
    private const int DefaultBufferSize = 1024 * 4;

    private readonly BufferedReadStream _stream;
    private readonly MultipartBoundary _boundary;
    private MultipartReaderStream _currentStream;

    /// <summary>
    /// Initializes a new instance of <see cref="MultipartReader"/>.
    /// </summary>
    /// <param name="boundary">The multipart boundary.</param>
    /// <param name="stream">The <see cref="Stream"/> containing multipart data.</param>
    public MultipartReader(string boundary, Stream stream)
        : this(boundary, stream, DefaultBufferSize)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="MultipartReader"/>.
    /// </summary>
    /// <param name="boundary">The multipart boundary.</param>
    /// <param name="stream">The <see cref="Stream"/> containing multipart data.</param>
    /// <param name="bufferSize">The minimum buffer size to use.</param>
    public MultipartReader(string boundary, Stream stream, int bufferSize)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        ArgumentNullException.ThrowIfNull(stream);

        if (bufferSize < boundary.Length + 8) // Size of the boundary + leading and trailing CRLF + leading and trailing '--' markers.
        {
            throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "Insufficient buffer space, the buffer must be larger than the boundary: " + boundary);
        }
        _stream = new BufferedReadStream(stream, bufferSize);
        boundary = HeaderUtilities.RemoveQuotes(new StringSegment(boundary)).ToString();
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
    /// The optional limit for the body length of each multipart section.
    /// The hosting server is responsible for limiting the overall body length.
    /// </summary>
    public long? BodyLengthLimit { get; set; }

    /// <summary>
    /// Reads the next <see cref="MultipartSection"/>.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.
    /// The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns></returns>
    public async Task<MultipartSection?> ReadNextSectionAsync(CancellationToken cancellationToken = new CancellationToken())
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
        _boundary.ExpectLeadingCrlf();
        _currentStream = new MultipartReaderStream(_stream, _boundary) { LengthLimit = BodyLengthLimit };
        long? baseStreamOffset = _stream.CanSeek ? (long?)_stream.Position : null;
        return new MultipartSection() { Headers = headers, Body = _currentStream, BaseStreamOffset = baseStreamOffset };
    }

    private async Task<Dictionary<string, StringValues>> ReadHeadersAsync(CancellationToken cancellationToken)
    {
        int totalSize = 0;
        var accumulator = new KeyValueAccumulator();
        var line = await _stream.ReadLineAsync(HeadersLengthLimit, cancellationToken);
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// DEFLATE decompression provider.
/// </summary>
/// <remarks>
/// As described in RFC 2616, the deflate content-coding token represents the "zlib" format
/// (RFC 1950) in combination with the "deflate" compression algorithm (RFC 1951).
/// </remarks>
internal sealed class DeflateDecompressionProvider : IDecompressionProvider
{
    /// <inheritdoc />
    public Stream GetDecompressionStream(Stream stream)
    {
        return new ZLibStream(stream, CompressionMode.Decompress, leaveOpen: true);
    }
}

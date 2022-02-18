// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// GZip decompression provider.
/// </summary>
public sealed class GzipDecompressionProvider : IDecompressionProvider
{
    /// <inheritdoc />
    public string EncodingName => "gzip";

    /// <inheritdoc />
    public Stream CreateStream(Stream outputStream)
    {
        return new GZipStream(outputStream, CompressionMode.Decompress);
    }
}

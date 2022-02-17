// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// DEFLATE decompression provider.
/// </summary>
public class DeflateDecompressionProvider : IDecompressionProvider
{
    /// <inheritdoc />
    public string EncodingName => "deflate";

    /// <inheritdoc />
    public Stream CreateStream(Stream outputStream)
    {
        return new DeflateStream(outputStream, CompressionMode.Decompress);
    }
}

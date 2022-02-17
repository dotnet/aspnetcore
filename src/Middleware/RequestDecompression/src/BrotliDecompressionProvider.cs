// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// Brotli decompression provider.
/// </summary>
public class BrotliDecompressionProvider : IDecompressionProvider
{
    /// <inheritdoc />
    public string EncodingName => "br";

    /// <inheritdoc />
    public Stream CreateStream(Stream outputStream)
    {
        return new BrotliStream(outputStream, CompressionMode.Decompress);
    }
}

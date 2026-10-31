// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// Brotli decompression provider.
/// </summary>
internal sealed class BrotliDecompressionProvider : IDecompressionProvider
{
    /// <inheritdoc />
    public Stream GetDecompressionStream(Stream stream)
    {
        return new BrotliStream(stream, CompressionMode.Decompress, leaveOpen: true);
    }
}

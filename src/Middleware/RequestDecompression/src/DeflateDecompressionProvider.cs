// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// DEFLATE decompression provider.
/// </summary>
internal sealed class DeflateDecompressionProvider : IDecompressionProvider
{
    /// <inheritdoc />
    public Stream GetDecompressionStream(Stream stream)
    {
        return new DeflateStream(stream, CompressionMode.Decompress, leaveOpen: true);
    }
}

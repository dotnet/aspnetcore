// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// GZip decompression provider.
/// </summary>
internal sealed class GZipDecompressionProvider : IDecompressionProvider
{
    /// <inheritdoc />
    public Stream GetDecompressionStream(Stream stream)
    {
        return new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
    }
}

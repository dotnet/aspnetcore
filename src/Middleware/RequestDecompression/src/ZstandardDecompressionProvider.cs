// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// Zstandard decompression provider.
/// </summary>
internal sealed class ZstandardDecompressionProvider : IDecompressionProvider
{
    // Cap the decompression window at 8 MB (2^23). RFC 9659 requires the "zstd" HTTP content
    // coding to use a window no larger than 8 MB, and browsers such as Chromium reject larger
    // windows for the same reason. Payloads produced with a larger window (for example,
    // "zstd --long") will fail to decompress; default streaming compression uses a window of at
    // most 2 MB and is unaffected.
    private static readonly ZstandardDecompressionOptions s_decompressionOptions = new()
    {
        MaxWindowLog = 23,
    };

    /// <inheritdoc />
    public Stream GetDecompressionStream(Stream stream)
    {
        return new ZstandardStream(stream, s_decompressionOptions, leaveOpen: true);
    }
}

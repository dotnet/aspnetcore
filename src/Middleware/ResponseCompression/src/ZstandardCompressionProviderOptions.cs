// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ResponseCompression;

/// <summary>
/// Options for the <see cref="ZstandardCompressionProvider"/>
/// </summary>
public class ZstandardCompressionProviderOptions : IOptions<ZstandardCompressionProviderOptions>
{
    private static readonly ZstandardCompressionOptions DefaultZstandardCompressionOptions = new()
    {
        Quality = 1 // Fastest compression level
    };

    /// <summary>
    /// The compression options to use for the stream. Default quality is 1 (fastest compression level).
    /// </summary>
    public ZstandardCompressionOptions CompressionOptions { get; set; } = DefaultZstandardCompressionOptions;

    /// <inheritdoc />
    ZstandardCompressionProviderOptions IOptions<ZstandardCompressionProviderOptions>.Value => this;
}

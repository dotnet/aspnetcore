// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ResponseCompression;

/// <summary>
/// Options for the GzipCompressionProvider
/// </summary>
public class GzipCompressionProviderOptions : IOptions<GzipCompressionProviderOptions>
{
    /// <summary>
    /// What level of compression to use for the stream. The default is Fastest.
    /// </summary>
    public CompressionLevel Level { get; set; } = CompressionLevel.Fastest;

    /// <inheritdoc />
    GzipCompressionProviderOptions IOptions<GzipCompressionProviderOptions>.Value => this;
}

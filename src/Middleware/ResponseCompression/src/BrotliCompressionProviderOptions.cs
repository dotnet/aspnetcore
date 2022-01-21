// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ResponseCompression;

/// <summary>
/// Options for the <see cref="BrotliCompressionProvider"/>
/// </summary>
public class BrotliCompressionProviderOptions : IOptions<BrotliCompressionProviderOptions>
{
    /// <summary>
    /// What level of compression to use for the stream. The default is <see cref="CompressionLevel.Fastest"/>.
    /// </summary>
    public CompressionLevel Level { get; set; } = CompressionLevel.Fastest;

    /// <inheritdoc />
    BrotliCompressionProviderOptions IOptions<BrotliCompressionProviderOptions>.Value => this;
}

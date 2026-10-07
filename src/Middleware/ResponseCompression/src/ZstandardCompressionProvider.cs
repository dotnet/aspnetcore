// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ResponseCompression;

/// <summary>
/// Zstandard compression provider.
/// </summary>
public class ZstandardCompressionProvider : ICompressionProvider
{
    /// <summary>
    /// Creates a new instance of <see cref="ZstandardCompressionProvider"/> with options.
    /// </summary>
    /// <param name="options">The options for this instance.</param>
    public ZstandardCompressionProvider(IOptions<ZstandardCompressionProviderOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Options = options.Value;
    }

    private ZstandardCompressionProviderOptions Options { get; }

    /// <inheritdoc />
    public string EncodingName { get; } = "zstd";

    /// <inheritdoc />
    public bool SupportsFlush { get; } = true;

    /// <inheritdoc />
    public Stream CreateStream(Stream outputStream)
    {
        return new ZstandardStream(outputStream, Options.CompressionOptions, leaveOpen: true);
    }
}

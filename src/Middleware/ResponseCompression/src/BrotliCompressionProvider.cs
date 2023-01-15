// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ResponseCompression;

/// <summary>
/// Brotli compression provider.
/// </summary>
public class BrotliCompressionProvider : ICompressionProvider
{
    /// <summary>
    /// Creates a new instance of <see cref="BrotliCompressionProvider"/> with options.
    /// </summary>
    /// <param name="options">The options for this instance.</param>
    public BrotliCompressionProvider(IOptions<BrotliCompressionProviderOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Options = options.Value;
    }

    private BrotliCompressionProviderOptions Options { get; }

    /// <inheritdoc />
    public string EncodingName { get; } = "br";

    /// <inheritdoc />
    public bool SupportsFlush { get; } = true;

    /// <inheritdoc />
    public Stream CreateStream(Stream outputStream)
    {
        return new BrotliStream(outputStream, Options.Level, leaveOpen: true);
    }
}

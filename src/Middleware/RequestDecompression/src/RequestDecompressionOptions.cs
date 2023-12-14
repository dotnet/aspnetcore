// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// Options for the HTTP request decompression middleware.
/// </summary>
public sealed class RequestDecompressionOptions
{
    /// <summary>
    /// The <see cref="IDecompressionProvider"/> types to use for request decompression.
    /// </summary>
    public IDictionary<string, IDecompressionProvider> DecompressionProviders { get; } = new Dictionary<string, IDecompressionProvider>(StringComparer.OrdinalIgnoreCase)
    {
        ["br"] = new BrotliDecompressionProvider(),
        ["deflate"] = new DeflateDecompressionProvider(),
        ["gzip"] = new GZipDecompressionProvider()
    };
}

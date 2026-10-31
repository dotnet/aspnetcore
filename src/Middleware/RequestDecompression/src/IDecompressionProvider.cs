// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// Provides a specific decompression implementation to decompress HTTP request bodies.
/// </summary>
public interface IDecompressionProvider
{
    /// <summary>
    /// Creates a new decompression stream.
    /// </summary>
    /// <param name="stream">The compressed request body stream.</param>
    /// <returns>The decompression stream.</returns>
    Stream GetDecompressionStream(Stream stream);
}

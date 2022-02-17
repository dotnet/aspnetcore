// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// Provides a specific decompression implementation to decompress HTTP requests.
/// </summary>
public interface IDecompressionProvider
{
    /// <summary>
    /// The encoding name used in the 'Content-Encoding' request header.
    /// </summary>
    string EncodingName { get; }

    /// <summary>
    /// Creates a new decompression stream.
    /// </summary>
    /// <param name="outputStream">The stream where the decompressed data will be written.</param>
    /// <returns>The decompression stream.</returns>
    Stream CreateStream(Stream outputStream);
}

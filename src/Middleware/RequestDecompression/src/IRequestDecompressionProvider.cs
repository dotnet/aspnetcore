// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// Used to examine requests to see if decompression should be used.
/// </summary>
public interface IRequestDecompressionProvider
{
    /// <summary>
    /// Examines the request and selects an acceptable decompression provider, if any.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>A decompression provider or null if there are no acceptable providers.</returns>
    IDecompressionProvider? GetDecompressionProvider(HttpContext context);

    /// <summary>
    /// Examines the request to see if it should be decompressed.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns><see langword="true"/> if the request should be decompressed, otherwise <see langword="false"/>.</returns>
    bool ShouldDecompressRequest(HttpContext context);

    /// <summary>
    /// Examines the request to see if decompression is supported for the specified Content-Type.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns><see langword="true"/> if the Content-Encoding is supported, otherwise <see langword="false"/>.</returns>
    bool IsContentEncodingSupported(HttpContext context);
}

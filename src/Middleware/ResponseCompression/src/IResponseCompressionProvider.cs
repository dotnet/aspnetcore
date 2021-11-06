// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ResponseCompression;

/// <summary>
/// Used to examine requests and responses to see if compression should be enabled.
/// </summary>
public interface IResponseCompressionProvider
{
    /// <summary>
    /// Examines the request and selects an acceptable compression provider, if any.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>A compression provider or null if compression should not be used.</returns>
    ICompressionProvider? GetCompressionProvider(HttpContext context);

    /// <summary>
    /// Examines the response on first write to see if compression should be used.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns><see langword="true" /> if the response should be compressed, otherwise <see langword="false" />.</returns>
    bool ShouldCompressResponse(HttpContext context);

    /// <summary>
    /// Examines the request to see if compression should be used for response.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns><see langword="true" /> if the request accepts compression, otherwise <see langword="false" />.</returns>
    bool CheckRequestAcceptsCompression(HttpContext context);
}

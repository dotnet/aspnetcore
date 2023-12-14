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
    /// <returns>The decompression stream when the provider is capable of decompressing the HTTP request body, otherwise <see langword="null" />.</returns>
    Stream? GetDecompressionStream(HttpContext context);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// Options for the HTTP request decompression middleware.
/// </summary>
public class RequestDecompressionOptions
{
    /// <summary>
    /// The <see cref="IDecompressionProvider"/> types to use for request decompression.
    /// </summary>
    public DecompressionProviderCollection Providers { get; } = new DecompressionProviderCollection();
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.RequestDecompression;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for the HTTP request decompression middleware.
/// </summary>
public static class RequestDecompressionBuilderExtensions
{
    /// <summary>
    /// Adds middleware for dynamically decompressing HTTP request bodies.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
    public static IApplicationBuilder UseRequestDecompression(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseMiddleware<RequestDecompressionMiddleware>();
    }
}

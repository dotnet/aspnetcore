// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.ResponseCompression;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for the ResponseCompression middleware.
/// </summary>
public static class ResponseCompressionBuilderExtensions
{
    /// <summary>
    /// Adds middleware for dynamically compressing HTTP Responses.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
    public static IApplicationBuilder UseResponseCompression(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseMiddleware<ResponseCompressionMiddleware>();
    }
}

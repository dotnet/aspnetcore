// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.ResponseCompression;

/// <summary>
/// Defaults for the ResponseCompressionMiddleware
/// </summary>
public class ResponseCompressionDefaults
{
    /// <summary>
    /// Default MIME types to compress responses for.
    /// </summary>
    // This list is not intended to be exhaustive, it's a baseline for the 90% case.
    public static readonly IEnumerable<string> MimeTypes = new[]
    {
            // General
            "text/plain",
            // Static files
            "text/css",
            "application/javascript",
            "text/javascript",
            // MVC
            "text/html",
            "application/xml",
            "text/xml",
            "application/json",
            "text/json",
            // WebAssembly
            "application/wasm",
        };
}

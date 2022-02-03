// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// Contains information used by the handler of the <see cref="StatusCodePagesMiddleware"/>.
/// </summary>
public class StatusCodeContext
{
    /// <summary>
    /// Creates a new <see cref="StatusCodeContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="options">The configured <see cref="StatusCodePagesOptions"/>.</param>
    /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
    public StatusCodeContext(HttpContext context, StatusCodePagesOptions options, RequestDelegate next)
    {
        HttpContext = context;
        Options = options;
        Next = next;
    }

    /// <summary>
    /// Gets the <see cref="HttpContext"/>.
    /// </summary>
    public HttpContext HttpContext { get; private set; }

    /// <summary>
    /// Gets the configured <see cref="StatusCodePagesOptions"/>.
    /// </summary>
    public StatusCodePagesOptions Options { get; private set; }

    /// <summary>
    /// Gets the <see cref="RequestDelegate"/> representing the next middleware in the pipeline.
    /// </summary>
    public RequestDelegate Next { get; private set; }
}

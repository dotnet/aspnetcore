// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// Provides context about the error currently being handled by the DeveloperExceptionPageMiddleware.
/// </summary>
public class ErrorContext
{
    /// <summary>
    /// Initializes the ErrorContext with the specified <see cref="HttpContext"/> and <see cref="Exception"/>.
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="exception"></param>
    public ErrorContext(HttpContext httpContext, Exception exception)
    {
        HttpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }

    /// <summary>
    /// The <see cref="HttpContext"/>.
    /// </summary>
    public HttpContext HttpContext { get; }

    /// <summary>
    /// The <see cref="Exception"/> thrown during request processing.
    /// </summary>
    public Exception Exception { get; }
}

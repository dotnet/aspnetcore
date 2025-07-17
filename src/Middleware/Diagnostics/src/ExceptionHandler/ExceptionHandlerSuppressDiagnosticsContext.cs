// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// The context used to determine whether <see cref="ExceptionHandlerMiddleware"/> should record diagnostics for an exception.
/// </summary>
public sealed class ExceptionHandlerSuppressDiagnosticsContext
{
    /// <summary>
    /// Gets the <see cref="Http.HttpContext"/> of the current request.
    /// </summary>
    public required HttpContext HttpContext { get; init; }

    /// <summary>
    /// Gets the <see cref="System.Exception"/> that the exception handler middleware is processing.
    /// </summary>
    public required Exception Exception { get; init; }

    /// <summary>
    /// Gets the result of exception handling by <see cref="ExceptionHandlerMiddleware"/>.
    /// </summary>
    public required ExceptionHandledType ExceptionHandledBy { get; init; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// The context used to determine whether exception handler middleware should log an exception.
/// </summary>
public sealed class ExceptionHandlerSuppressLoggingContext
{
    /// <summary>
    /// Gets the <see cref="System.Exception"/> that the exception handler middleware is processing.
    /// </summary>
    public required Exception Exception { get; init; }

    /// <summary>
    /// Gets the result of the exception handler middleware.
    /// </summary>
    public required ExceptionHandlerResult HandlerResult { get; init; }
}

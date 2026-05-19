// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// The result of handling an exception with the <see cref="ExceptionHandlerMiddleware"/>.
/// </summary>
public enum ExceptionHandledType
{
    /// <summary>
    /// Exception was unhandled.
    /// </summary>
    Unhandled,
    /// <summary>
    /// Exception was handled by an <see cref="Diagnostics.IExceptionHandler"/> service instance registered in the DI container.
    /// </summary>
    ExceptionHandlerService,
    /// <summary>
    /// Exception was handled by an <see cref="Http.IProblemDetailsService"/> instance registered in the DI container.
    /// </summary>
    ProblemDetailsService,
    /// <summary>
    /// Exception was handled by by <see cref="Builder.ExceptionHandlerOptions.ExceptionHandler"/>.
    /// </summary>
    ExceptionHandlerDelegate,
    /// <summary>
    /// Exception was handled by by <see cref="Builder.ExceptionHandlerOptions.ExceptionHandlingPath"/>.
    /// </summary>
    ExceptionHandlingPath
}

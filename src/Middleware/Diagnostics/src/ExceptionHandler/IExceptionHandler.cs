// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// Represents an interface for handling exceptions in ASP.NET Core applications.
/// `IExceptionHandler` implementations are used by the exception handler middleware.
/// </summary>
public interface IExceptionHandler
{
    /// <summary>
    /// Tries to handle the specified exception asynchronously within the ASP.NET Core pipeline.
    /// Implementations of this method can provide custom exception-handling logic for different scenarios. 
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> for the request.</param>
    /// <param name="exception">The unhandled exception.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous read operation. The value of its <see cref="P:System.Threading.Tasks.ValueTask`1.Result" />
    /// property contains the result of the handling operation.
    /// <see langword="true"/> if the exception was handled successfully; otherwise <see langword="false"/>.
    /// </returns>
    ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken);
}

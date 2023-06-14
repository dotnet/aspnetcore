// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// Represents an interface for handling exceptions in ASP.NET Core applications.
/// Implementations of this interface can provide custom exception handling logic for
/// different scenarios in the application.
/// </summary>
public interface IExceptionHandler
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="exception"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken);
}

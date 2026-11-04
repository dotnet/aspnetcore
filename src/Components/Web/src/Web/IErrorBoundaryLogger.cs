// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

// The reason this abstraction exists is that logging behaviors differ across hosting platforms.
// For example, Blazor Server logs to both the server and client, whereas WebAssembly has only one log.

/// <summary>
/// Logs exception information for a <see cref="ErrorBoundary"/> component.
/// </summary>
public interface IErrorBoundaryLogger
{
    /// <summary>
    /// Logs the supplied <paramref name="exception"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to log.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    ValueTask LogErrorAsync(Exception exception);
}

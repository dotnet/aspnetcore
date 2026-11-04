// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

internal sealed class WebAssemblyErrorBoundaryLogger : IErrorBoundaryLogger
{
    private readonly ILogger<ErrorBoundary> _errorBoundaryLogger;

    public WebAssemblyErrorBoundaryLogger(ILogger<ErrorBoundary> errorBoundaryLogger)
    {
        _errorBoundaryLogger = errorBoundaryLogger ?? throw new ArgumentNullException(nameof(errorBoundaryLogger));
    }

    public ValueTask LogErrorAsync(Exception exception)
    {
        // For, client-side code, all internal state is visible to the end user. We can just
        // log directly to the console.
        _errorBoundaryLogger.LogError(exception, exception.ToString());
        return ValueTask.CompletedTask;
    }
}

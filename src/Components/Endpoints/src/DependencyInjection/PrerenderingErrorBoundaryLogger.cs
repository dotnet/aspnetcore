// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class PrerenderingErrorBoundaryLogger : IErrorBoundaryLogger
{
    private static readonly Action<ILogger, string, Exception> _exceptionCaughtByErrorBoundary = LoggerMessage.Define<string>(
        LogLevel.Warning,
        100,
        "Unhandled exception rendering component: {Message}");

    private readonly ILogger _logger;

    public PrerenderingErrorBoundaryLogger(ILogger<ErrorBoundary> logger)
    {
        _logger = logger;
    }

    public ValueTask LogErrorAsync(Exception exception)
    {
        _exceptionCaughtByErrorBoundary(_logger, exception.Message, exception);
        return ValueTask.CompletedTask;
    }
}

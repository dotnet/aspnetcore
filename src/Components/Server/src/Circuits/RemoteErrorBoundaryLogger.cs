// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

internal sealed partial class RemoteErrorBoundaryLogger : IErrorBoundaryLogger
{
    private readonly ILogger _logger;
    private readonly IJSRuntime _jsRuntime;
    private readonly CircuitOptions _options;

    public RemoteErrorBoundaryLogger(ILogger<ErrorBoundary> logger, IJSRuntime jsRuntime, IOptions<CircuitOptions> options)
    {
        _logger = logger;
        _jsRuntime = jsRuntime;
        _options = options.Value;
    }

    public ValueTask LogErrorAsync(Exception exception)
    {
        // We always log detailed information to the server-side log
        Log.ExceptionCaughtByErrorBoundary(_logger, exception.Message, exception);

        // We log to the client only if the browser is connected interactively, and even then
        // we may suppress the details
        var shouldLogToClient = (_jsRuntime as RemoteJSRuntime)?.IsInitialized == true;
        if (shouldLogToClient)
        {
            var message = _options.DetailedErrors
                ? exception.ToString()
                : $"For more details turn on detailed exceptions in '{nameof(CircuitOptions)}.{nameof(CircuitOptions.DetailedErrors)}'";
            return _jsRuntime.InvokeVoidAsync("console.error", message);
        }
        else
        {
            return ValueTask.CompletedTask;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(100, LogLevel.Warning, "Unhandled exception rendering component: {Message}", EventName = "ExceptionCaughtByErrorBoundary")]
        public static partial void ExceptionCaughtByErrorBoundary(ILogger logger, string message, Exception exception);
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class RemoteErrorBoundaryLogger : IErrorBoundaryLogger
    {
        private static readonly Action<ILogger, string, Exception> _exceptionCaughtByErrorBoundary = LoggerMessage.Define<string>(
            LogLevel.Warning,
            100,
            "Unhandled exception rendering component: {Message}");

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
            _exceptionCaughtByErrorBoundary(_logger, exception.Message, exception);

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
    }
}

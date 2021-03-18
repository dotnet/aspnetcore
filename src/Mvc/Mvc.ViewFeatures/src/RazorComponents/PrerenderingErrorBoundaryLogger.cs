// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal class PrerenderingErrorBoundaryLogger : IErrorBoundaryLogger
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

        public ValueTask LogErrorAsync(Exception exception, bool clientOnly)
        {
            _exceptionCaughtByErrorBoundary(_logger, exception.Message, exception);
            return ValueTask.CompletedTask;
        }
    }
}

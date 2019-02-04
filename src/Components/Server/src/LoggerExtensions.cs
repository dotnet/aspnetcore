// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Server
{
    internal static class LoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _unhandledExceptionRenderingComponent;

        static LoggerExtensions()
        {
            _unhandledExceptionRenderingComponent = LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(1, "ExceptionRenderingComponent"),
                "Unhandled exception rendering component: {Message}");
        }

        public static void UnhandledExceptionRenderingComponent(this ILogger logger, Exception exception)
        {
            _unhandledExceptionRenderingComponent(
                logger,
                exception.Message,
                exception);
        }
    }
}

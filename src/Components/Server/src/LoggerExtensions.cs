// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Server
{
    internal static class LoggerExtensions
    {
        private static readonly Action<ILogger, string, int, string, Exception> _unhandledExceptionRenderingComponent;

        static LoggerExtensions()
        {
            _unhandledExceptionRenderingComponent = LoggerMessage.Define<string, int, string>(
                LogLevel.Warning,
                new EventId(1, "ExceptionRenderingComponent"),
                "Unhandled exception rendering component '{ComponentType}({ComponentId}): {Message}");
        }

        public static void UnhandledExceptionRenderingComponent(this ILogger logger, Type componentType, int componentId, Exception exception)
        {
            _unhandledExceptionRenderingComponent(
                logger,
                TypeNameHelper.GetTypeDisplayName(componentType),
                componentId,
                exception.Message,
                exception);
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    internal static class HostingLoggerExtensions
    {
        public static IDisposable RequestScope(this ILogger logger, HttpContext httpContext, string correlationId)
        {
            return logger.BeginScope(new HostingLogScope(httpContext, correlationId));
        }

        public static void ApplicationError(this ILogger logger, Exception exception)
        {
            logger.ApplicationError(
                eventId: LoggerEventIds.ApplicationStartupException,
                message: "Application startup exception",
                exception: exception);
        }

        public static void HostingStartupAssemblyError(this ILogger logger, Exception exception)
        {
            logger.ApplicationError(
                eventId: LoggerEventIds.HostingStartupAssemblyException,
                message: "Hosting startup assembly exception",
                exception: exception);
        }

        public static void ApplicationError(this ILogger logger, EventId eventId, string message, Exception exception)
        {
            var reflectionTypeLoadException = exception as ReflectionTypeLoadException;
            if (reflectionTypeLoadException != null)
            {
                foreach (var ex in reflectionTypeLoadException.LoaderExceptions)
                {
                    message = message + Environment.NewLine + ex.Message;
                }
            }

            logger.LogCritical(
                eventId: eventId,
                message: message,
                exception: exception);
        }

        public static void Starting(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                   eventId: LoggerEventIds.Starting,
                   message: "Hosting starting");
            }
        }

        public static void Started(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    eventId: LoggerEventIds.Started,
                    message: "Hosting started");
            }
        }

        public static void Shutdown(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    eventId: LoggerEventIds.Shutdown,
                    message: "Hosting shutdown");
            }
        }

        public static void ServerShutdownException(this ILogger logger, Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    eventId: LoggerEventIds.ServerShutdownException,
                    exception: ex,
                    message: "Server shutdown exception");
            }
        }

        private class HostingLogScope : IReadOnlyList<KeyValuePair<string, object>>
        {
            private readonly HttpContext _httpContext;
            private readonly string _correlationId;

            private string _cachedToString;

            public int Count
            {
                get
                {
                    return 3;
                }
            }

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    if (index == 0)
                    {
                        return new KeyValuePair<string, object>("RequestId", _httpContext.TraceIdentifier);
                    }
                    else if (index == 1)
                    {
                        return new KeyValuePair<string, object>("RequestPath", _httpContext.Request.Path.ToString());
                    }
                    else if (index == 2)
                    {
                        return new KeyValuePair<string, object>("CorrelationId", _correlationId);
                    }

                    throw new ArgumentOutOfRangeException(nameof(index));
                }
            }

            public HostingLogScope(HttpContext httpContext, string correlationId)
            {
                _httpContext = httpContext;
                _correlationId = correlationId;
            }

            public override string ToString()
            {
                if (_cachedToString == null)
                {
                    _cachedToString = string.Format(
                        CultureInfo.InvariantCulture,
                        "RequestId:{0} RequestPath:{1}",
                        _httpContext.TraceIdentifier,
                        _httpContext.Request.Path);
                }

                return _cachedToString;
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                for (int i = 0; i < Count; ++i)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}


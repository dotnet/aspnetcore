// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services
{
    internal class Logger : ILogger
    {
        public WebAssemblyLoggerInformation[] Loggers { get; set; }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var loggers = Loggers;
            if (loggers == null)
            {
                return;
            }

            List<Exception> exceptions = null;
            for (var i = 0; i < loggers.Length; i++)
            {
                ref readonly var loggerInfo = ref loggers[i];
                var logger = loggerInfo.Logger;
                if (!logger.IsEnabled(logLevel))
                {
                    continue;
                }

                LoggerLog(logLevel, eventId, logger, exception, formatter, ref exceptions, state);
            }

            if (exceptions != null && exceptions.Count > 0)
            {
                ThrowLoggingError(exceptions);
            }

            static void LoggerLog(LogLevel logLevel, EventId eventId, ILogger logger, Exception exception, Func<TState, Exception, string> formatter, ref List<Exception> exceptions, in TState state)
            {
                try
                {
                    logger.Log(logLevel, eventId, state, exception, formatter);
                }
                catch (Exception ex)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(ex);
                }
            }
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            var loggers = Loggers;
            if (loggers == null)
            {
                return false;
            }

            List<Exception> exceptions = null;
            var i = 0;
            for (; i < loggers.Length; i++)
            {
                ref readonly var loggerInfo = ref loggers[i];
                var logger = loggerInfo.Logger;
                if (!logger.IsEnabled(logLevel))
                {
                    continue;
                }

                if (LoggerIsEnabled(logLevel, loggerInfo.Logger, ref exceptions))
                {
                    break;
                }
            }

            if (exceptions != null && exceptions.Count > 0)
            {
                ThrowLoggingError(exceptions);
            }

            return i < loggers.Length ? true : false;

            static bool LoggerIsEnabled(LogLevel logLevel, ILogger logger, ref List<Exception> exceptions)
            {
                try
                {
                    if (logger.IsEnabled(logLevel))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(ex);
                }

                return false;
            }
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state)
        {
            return NoOpDisposable.Instance;
        }

        private static void ThrowLoggingError(List<Exception> exceptions)
        {
            throw new AggregateException(
                message: "An error occurred while writing to logger(s).", innerExceptions: exceptions);
        }
    }

    public class NoOpDisposable : IDisposable
    {
        public static NoOpDisposable Instance = new NoOpDisposable();

        public void Dispose() { }
    }
}

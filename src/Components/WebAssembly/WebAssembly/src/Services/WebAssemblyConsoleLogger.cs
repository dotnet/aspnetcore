// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services
{
    internal class WebAssemblyConsoleLogger<T> : ILogger<T>, ILogger
    {
        private readonly IJSInProcessRuntime _jsRuntime;

        public WebAssemblyConsoleLogger(IJSInProcessRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NoOpDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Warning;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var formattedMessage = formatter(state, exception);

            // If we're given an exception, we want to format it in a useful way, including
            // inner exceptions and stack traces. The default formatter doesn't do that.
            if (!(exception is null))
            {
                formattedMessage += FormatException(exception);
            }

            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    // Although https://console.spec.whatwg.org/#loglevel-severity claims that
                    // "console.debug" and "console.log" are synonyms, that doesn't match the
                    // behavior of browsers in the real world. Chromium only displays "debug"
                    // messages if you enable "Verbose" in the filter dropdown (which is off
                    // by default). As such "console.debug" is the best choice for messages
                    // with a lower severity level than "Information".
                    _jsRuntime.InvokeVoid("console.debug", formattedMessage);
                    break;
                case LogLevel.Information:
                    _jsRuntime.InvokeVoid("console.info", formattedMessage);
                    break;
                case LogLevel.Warning:
                    _jsRuntime.InvokeVoid("console.warn", formattedMessage);
                    break;
                case LogLevel.Error:
                    _jsRuntime.InvokeVoid("console.error", formattedMessage);
                    break;
                case LogLevel.Critical:
                    // Writing to Console.Error is even more severe than calling console.error,
                    // because it also causes the error UI (gold bar) to appear. We use Console.Error
                    // as the signal for triggering that because it's what the underlying dotnet.wasm
                    // runtime will do if it encounters a truly severe error outside the Blazor
                    // code paths.
                    Console.Error.WriteLine(formattedMessage);
                    break;
                default: // LogLevel.None or invalid enum values
                    Console.WriteLine(formattedMessage);
                    break;
            }
        }

        private static string FormatException(Exception exception)
        {
            var stringBuilder = new StringBuilder();
            AppendException(stringBuilder, exception);
            return stringBuilder.ToString();
        }

        private static void AppendException(StringBuilder stringBuilder, Exception exception)
        {
            AppendSingleException(stringBuilder, exception);

            if (exception is AggregateException aggregateException)
            {
                // If it's an AggregateException, just flatten and append them all, and we're done
                foreach (var flattenedInnerException in aggregateException.Flatten().InnerExceptions)
                {
                    AppendSingleException(stringBuilder, flattenedInnerException);
                }
            }
            else if (exception?.InnerException is Exception innerException)
            {
                // Otherwise, if it has an inner exception, recurse into it
                AppendException(stringBuilder, innerException);
            }
        }

        private static void AppendSingleException(StringBuilder stringBuilder, Exception exception)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendFormat("[{0}]: {1}", exception.GetType().FullName, exception.Message);

            if (exception.StackTrace != null)
            {
                stringBuilder.AppendLine();
                stringBuilder.Append(exception.StackTrace);
            }
        }

        private class NoOpDisposable : IDisposable
        {
            public static NoOpDisposable Instance = new NoOpDisposable();

            public void Dispose() { }
        }
    }
}

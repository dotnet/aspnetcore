// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
                case LogLevel.Critical:
                    _jsRuntime.InvokeVoid("console.error", formattedMessage);
                    break;
                default: // LogLevel.None or invalid enum values
                    Console.WriteLine($"[{logLevel}] {formattedMessage}");
                    break;
            }
        }

        private class NoOpDisposable : IDisposable
        {
            public static NoOpDisposable Instance = new NoOpDisposable();

            public void Dispose() { }
        }
    }
}

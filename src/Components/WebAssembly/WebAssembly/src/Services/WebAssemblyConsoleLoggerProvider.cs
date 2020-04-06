// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services
{
    /// <summary>
    /// A provider of <see cref="WebAssemblyConsoleLogger{T}"/> instances.
    /// </summary>
    internal class WebAssemblyConsoleLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, WebAssemblyConsoleLogger<object>> _loggers;
        private readonly IJSInProcessRuntime _jsRuntime;

        /// <summary>
        /// Creates an instance of <see cref="WebAssemblyConsoleLoggerProvider"/>.
        /// </summary>
        public WebAssemblyConsoleLoggerProvider(IJSInProcessRuntime jsRuntime)
        {
            _loggers = new ConcurrentDictionary<string, WebAssemblyConsoleLogger<object>>();
            _jsRuntime = jsRuntime;
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string name)
        {
            return _loggers.GetOrAdd(name, loggerName => new WebAssemblyConsoleLogger<object>(name, _jsRuntime));
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}

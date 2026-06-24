// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop.WebAssembly;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

/// <summary>
/// A provider of <see cref="WebAssemblyConsoleLogger{T}"/> instances.
/// </summary>
internal sealed class WebAssemblyConsoleLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, WebAssemblyConsoleLogger<object>> _loggers;
    private readonly WebAssemblyJSRuntime _jsRuntime;

    /// <summary>
    /// Creates an instance of <see cref="WebAssemblyConsoleLoggerProvider"/>.
    /// </summary>
    public WebAssemblyConsoleLoggerProvider(WebAssemblyJSRuntime jsRuntime)
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

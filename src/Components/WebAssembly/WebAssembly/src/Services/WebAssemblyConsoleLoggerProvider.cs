// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

/// <summary>
/// A provider of <see cref="WebAssemblyConsoleLogger{T}"/> instances.
/// </summary>
internal sealed class WebAssemblyConsoleLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, WebAssemblyConsoleLogger<object>> _loggers = new();

    /// <inheritdoc />
    public ILogger CreateLogger(string name)
    {
        return _loggers.GetOrAdd(name, static loggerName => new WebAssemblyConsoleLogger<object>(loggerName));
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}

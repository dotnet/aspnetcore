// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Tests;

public class ServerLogScope : IDisposable
{
    private readonly InProcessTestServer _serverFixture;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IDisposable _wrappedDisposable;
    private readonly ConcurrentDictionary<string, ILogger> _serverLoggers;
    private readonly ILogger _scopeLogger;
    private readonly object _lock;
    private bool _disposed;

    public ServerLogScope(InProcessTestServer serverFixture, ILoggerFactory loggerFactory, IDisposable wrappedDisposable)
    {
        _loggerFactory = loggerFactory;
        _serverFixture = serverFixture;
        _wrappedDisposable = wrappedDisposable;

        _lock = new object();

        _serverLoggers = new ConcurrentDictionary<string, ILogger>(StringComparer.Ordinal);
        _scopeLogger = _loggerFactory.CreateLogger(nameof(ServerLogScope));

        // Attach last after everything else is initialized because a logged error can happen at any time
        _serverFixture.ServerLogged += ServerFixtureOnServerLogged;

        _scopeLogger.LogInformation("Server log scope started.");
    }

    private void ServerFixtureOnServerLogged(LogRecord logRecord)
    {
        var write = logRecord.Write;

        if (write == null)
        {
            _scopeLogger.LogWarning("Server log has no data.");
            return;
        }

        ILogger logger;

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            // Create (or get) a logger with the same name as the server logger
            // Call in the lock to avoid ODE where LoggerFactory could be disposed by the wrapped disposable
            logger = _serverLoggers.GetOrAdd(write.LoggerName, loggerName => _loggerFactory.CreateLogger("SERVER " + loggerName));
        }

        logger.Log(write.LogLevel, write.EventId, write.State, write.Exception, write.Formatter);
    }

    public void Dispose()
    {
        _serverFixture.ServerLogged -= ServerFixtureOnServerLogged;

        _scopeLogger.LogInformation("Server log scope stopped.");

        lock (_lock)
        {
            _wrappedDisposable?.Dispose();
            _disposed = true;
        }
    }
}

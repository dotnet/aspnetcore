// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Tests;

/// <summary>
/// A logger factory that will prepend the current SignalR connection ID to the message.
/// </summary>
public class WrappingLoggerFactory : ILoggerFactory
{
    private readonly ILoggerFactory _innerLoggerFactory;
    private readonly DummyProvider _provider;

    public WrappingLoggerFactory(ILoggerFactory innerLoggerFactory)
    {
        _innerLoggerFactory = innerLoggerFactory;
        _provider = new DummyProvider();
        AddProvider(_provider);
    }

    public void Dispose()
    {
        _innerLoggerFactory.Dispose();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new WrappingLogger(_provider, _innerLoggerFactory.CreateLogger(categoryName));
    }

    public void AddProvider(ILoggerProvider provider)
    {
        _innerLoggerFactory.AddProvider(provider);
    }

    private sealed class DummyProvider : ILoggerProvider, ISupportExternalScope
    {
        public IExternalScopeProvider ScopeProvider { get; private set; }

        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return NullLogger.Instance;
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            ScopeProvider = scopeProvider;
        }
    }

    private sealed class WrappingLogger : ILogger
    {
        private readonly DummyProvider _provider;
        private readonly ILogger _logger;

        public WrappingLogger(DummyProvider provider, ILogger logger)
        {
            _provider = provider;
            _logger = logger;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            // Build the message outside of the formatter
            // Serilog doesn't appear to use the formatter and just writes the state
            var connectionId = GetConnectionId();

            var sb = new StringBuilder();
            if (connectionId != null)
            {
                sb.Append(connectionId + " - ");
            }
            sb.Append(formatter(state, exception));
            var message = sb.ToString();

            _logger.Log(logLevel, eventId, message, exception, (s, ex) => s);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return _logger.BeginScope(state);
        }

        private string GetConnectionId()
        {
            string connectionId = null;
            _provider.ScopeProvider?.ForEachScope<object>((scope, s) =>
            {
                if (scope is IReadOnlyList<KeyValuePair<string, object>> logScope)
                {
                    if (logScope.FirstOrDefault(kv => kv.Key == "TransportConnectionId" || kv.Key == "ClientConnectionId").Value is string id)
                    {
                        connectionId = id;
                    }
                }
            }, null);
            return connectionId;
        }
    }
}

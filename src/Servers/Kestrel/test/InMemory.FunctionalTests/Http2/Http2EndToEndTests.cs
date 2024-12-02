// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.Http2;

public class Http2EndToEndTests : TestApplicationErrorLoggerLoggedTest
{
    [Fact]
    public async Task MiddlewareIsRunWithConnectionLoggingScopeForHttp2Requests()
    {
        var expectedLogMessage = "Log from connection scope!";
        string connectionIdFromFeature = null;

        var mockScopeLoggerProvider = new MockScopeLoggerProvider(expectedLogMessage);
        LoggerFactory.AddProvider(mockScopeLoggerProvider);

        await using var server = new TestServer(async context =>
        {
            connectionIdFromFeature = context.Features.Get<IConnectionIdFeature>().ConnectionId;

            var logger = context.RequestServices.GetRequiredService<ILogger<Http2EndToEndTests>>();
            logger.LogInformation(expectedLogMessage);

            await context.Response.WriteAsync("hello, world");
        },
        new TestServiceContext(LoggerFactory),
        listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http2;
        });

        var connectionCount = 0;
        using var connection = server.CreateConnection();

        using var socketsHandler = new SocketsHttpHandler()
        {
            ConnectCallback = (_, _) =>
            {
                if (connectionCount != 0)
                {
                    throw new InvalidOperationException();
                }

                connectionCount++;
                return new ValueTask<Stream>(connection.Stream);
            },
        };

        using var httpClient = new HttpClient(socketsHandler);

        using var httpRequestMessage = new HttpRequestMessage()
        {
            RequestUri = new Uri("http://localhost/"),
            Version = new Version(2, 0),
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
        };

        using var responseMessage = await httpClient.SendAsync(httpRequestMessage);

        Assert.Equal("hello, world", await responseMessage.Content.ReadAsStringAsync());

        Assert.NotNull(connectionIdFromFeature);
        Assert.NotNull(mockScopeLoggerProvider.ConnectionLogScope);
        Assert.Equal(connectionIdFromFeature, mockScopeLoggerProvider.ConnectionLogScope[0].Value);
    }

    private class MockScopeLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly string _expectedLogMessage;
        private IExternalScopeProvider _scopeProvider;

        public MockScopeLoggerProvider(string expectedLogMessage)
        {
            _expectedLogMessage = expectedLogMessage;
        }

        public ConnectionLogScope ConnectionLogScope { get; private set; }

        public ILogger CreateLogger(string categoryName)
        {
            return new MockScopeLogger(this);
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        public void Dispose()
        {
        }

        private class MockScopeLogger : ILogger
        {
            private readonly MockScopeLoggerProvider _loggerProvider;

            public MockScopeLogger(MockScopeLoggerProvider parent)
            {
                _loggerProvider = parent;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return _loggerProvider._scopeProvider?.Push(state);
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (formatter(state, exception) != _loggerProvider._expectedLogMessage)
                {
                    return;
                }

                _loggerProvider._scopeProvider?.ForEachScope(
                    (scopeObject, loggerPovider) =>
                    {
                        loggerPovider.ConnectionLogScope ??= scopeObject as ConnectionLogScope;
                    },
                    _loggerProvider);
            }
        }
    }
}

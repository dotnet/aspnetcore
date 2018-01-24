// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class ServerFixture<TStartup> : IDisposable
        where TStartup : class
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private IWebHost _host;
        private IApplicationLifetime _lifetime;
        private readonly IDisposable _logToken;
        private AsyncForwardingLoggerProvider _asyncLoggerProvider;

        public string WebSocketsUrl => Url.Replace("http", "ws");

        public string Url { get; private set; }

        public ServerFixture()
        {
            _asyncLoggerProvider = new AsyncForwardingLoggerProvider();

            var testLog = AssemblyTestLog.ForAssembly(typeof(TStartup).Assembly);
            _logToken = testLog.StartTestLog(null, $"{nameof(ServerFixture<TStartup>)}_{typeof(TStartup).Name}", out _loggerFactory, "ServerFixture");
            _loggerFactory.AddProvider(_asyncLoggerProvider);
            _logger = _loggerFactory.CreateLogger<ServerFixture<TStartup>>();
            // We're using 127.0.0.1 instead of localhost to ensure that we use IPV4 across different OSes
            Url = "http://127.0.0.1:" + GetNextPort();

            StartServer(Url);
        }

        public void SetTestLoggerFactory(ILoggerFactory loggerFactory)
        {
            _asyncLoggerProvider.SetLoggerFactory(loggerFactory);
        }

        private void StartServer(string url)
        {
            _host = new WebHostBuilder()
                .ConfigureLogging(builder => builder.AddProvider(new ForwardingLoggerProvider(_loggerFactory)))
                .UseStartup(typeof(TStartup))
                .UseKestrel()
                .UseUrls(url)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .Build();

            var t = Task.Run(() => _host.Start());
            _logger.LogInformation("Starting test server...");
            _lifetime = _host.Services.GetRequiredService<IApplicationLifetime>();
            if (!_lifetime.ApplicationStarted.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)))
            {
                // t probably faulted
                if (t.IsFaulted)
                {
                    throw t.Exception.InnerException;
                }
                throw new TimeoutException("Timed out waiting for application to start.");
            }
            _logger.LogInformation("Test Server started");

            _lifetime.ApplicationStopped.Register(() =>
            {
                _logger.LogInformation("Test server shut down");
                _logToken.Dispose();
            });
        }

        public void Dispose()
        {
            _logger.LogInformation("Shutting down test server");
            _host.Dispose();
            _loggerFactory.Dispose();
        }

        private class AsyncForwardingLoggerProvider : ILoggerProvider
        {
            private AsyncLocal<ILoggerFactory> _localLogger = new AsyncLocal<ILoggerFactory>();

            public ILogger CreateLogger(string categoryName)
            {
                return new AsyncLocalForwardingLogger(categoryName, _localLogger);
            }

            public void Dispose()
            {
            }

            public void SetLoggerFactory(ILoggerFactory loggerFactory)
            {
                _localLogger.Value = loggerFactory;
            }

            private class AsyncLocalForwardingLogger : ILogger
            {
                private string _categoryName;
                private AsyncLocal<ILoggerFactory> _localLoggerFactory;

                public AsyncLocalForwardingLogger(string categoryName, AsyncLocal<ILoggerFactory> localLoggerFactory)
                {
                    _categoryName = categoryName;
                    _localLoggerFactory = localLoggerFactory;
                }

                public IDisposable BeginScope<TState>(TState state)
                {
                    return GetLocalLogger().BeginScope(state);
                }

                public bool IsEnabled(LogLevel logLevel)
                {
                    return GetLocalLogger().IsEnabled(logLevel);
                }

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                {
                    GetLocalLogger().Log(logLevel, eventId, state, exception, formatter);
                }

                private ILogger GetLocalLogger()
                {
                    var factory = _localLoggerFactory.Value;
                    if (factory == null)
                    {
                        return NullLogger.Instance;
                    }
                    return factory.CreateLogger(_categoryName);
                }
            }
        }

        private class ForwardingLoggerProvider : ILoggerProvider
        {
            private readonly ILoggerFactory _loggerFactory;

            public ForwardingLoggerProvider(ILoggerFactory loggerFactory)
            {
                _loggerFactory = loggerFactory;
            }

            public void Dispose()
            {
            }

            public ILogger CreateLogger(string categoryName)
            {
                return _loggerFactory.CreateLogger(categoryName);
            }
        }

        // Copied from https://github.com/aspnet/KestrelHttpServer/blob/47f1db20e063c2da75d9d89653fad4eafe24446c/test/Microsoft.AspNetCore.Server.Kestrel.FunctionalTests/AddressRegistrationTests.cs#L508
        private static int GetNextPort()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                // Let the OS assign the next available port. Unless we cycle through all ports
                // on a test run, the OS will always increment the port number when making these calls.
                // This prevents races in parallel test runs where a test is already bound to
                // a given port, and a new test is able to bind to the same port due to port
                // reuse being enabled by default by the OS.
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }
        }
    }
}

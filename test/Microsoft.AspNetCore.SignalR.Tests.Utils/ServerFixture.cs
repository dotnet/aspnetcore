// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        private readonly LogSinkProvider _logSinkProvider;

        public string WebSocketsUrl => Url.Replace("http", "ws");

        public string Url { get; private set; }

        public ServerFixture() : this(loggerFactory: null)
        {
        }

        public ServerFixture(ILoggerFactory loggerFactory)
        {
            _logSinkProvider = new LogSinkProvider();

            if (loggerFactory == null)
            {
                var testLog = AssemblyTestLog.ForAssembly(typeof(TStartup).Assembly);
                _logToken = testLog.StartTestLog(null, $"{nameof(ServerFixture<TStartup>)}_{typeof(TStartup).Name}",
                    out _loggerFactory, "ServerFixture");
            }
            else
            {
                _loggerFactory = loggerFactory;
            }

            _logger = _loggerFactory.CreateLogger<ServerFixture<TStartup>>();

            StartServer();
        }

        private void StartServer()
        {
            // We're using 127.0.0.1 instead of localhost to ensure that we use IPV4 across different OSes
            var url = "http://127.0.0.1:0";

            _host = new WebHostBuilder()
                .ConfigureLogging(builder => builder
                    .SetMinimumLevel(LogLevel.Debug)
                    .AddProvider(_logSinkProvider)
                    .AddProvider(new ForwardingLoggerProvider(_loggerFactory)))
                .UseStartup(typeof(TStartup))
                .UseKestrel()
                .UseUrls(url)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .Build();

            var t = Task.Run(() => _host.Start());
            _logger.LogInformation("Starting test server...");
            _lifetime = _host.Services.GetRequiredService<IApplicationLifetime>();

            // This only happens once per fixture, so we can afford to wait a little bit on it.
            if (!_lifetime.ApplicationStarted.WaitHandle.WaitOne(TimeSpan.FromSeconds(20)))
            {
                // t probably faulted
                if (t.IsFaulted)
                {
                    throw t.Exception.InnerException;
                }

                var logs = _logSinkProvider.GetLogs();
                throw new TimeoutException($"Timed out waiting for application to start.{Environment.NewLine}Startup Logs:{Environment.NewLine}{RenderLogs(logs)}");
            }
            _logger.LogInformation("Test Server started");

            // Get the URL from the server
            Url = _host.ServerFeatures.Get<IServerAddressesFeature>().Addresses.Single();

            _lifetime.ApplicationStopped.Register(() =>
            {
                _logger.LogInformation("Test server shut down");
                _logToken.Dispose();
            });
        }

        private string RenderLogs(IList<LogRecord> logs)
        {
            var builder = new StringBuilder();
            foreach (var log in logs)
            {
                builder.AppendLine($"{log.Timestamp:O} {log.Write.LoggerName} {log.Write.LogLevel}: {log.Write.Formatter(log.Write.State, log.Write.Exception)}");
                if (log.Write.Exception != null)
                {
                    var message = log.Write.Exception.ToString();
                    foreach (var line in message.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                    {
                        builder.AppendLine($"| {line}");
                    }
                }
            }
            return builder.ToString();
        }

        public void Dispose()
        {
            _logger.LogInformation("Shutting down test server");
            _host.Dispose();
            _loggerFactory.Dispose();
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
    }

    // TestSink doesn't seem to be thread-safe :(.
    internal class LogSinkProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<LogRecord> _logs = new ConcurrentQueue<LogRecord>();

        public ILogger CreateLogger(string categoryName)
        {
            return new LogSinkLogger(categoryName, this);
        }

        public void Dispose()
        {
        }

        public IList<LogRecord> GetLogs() => _logs.ToList();

        public void Log<TState>(string categoryName, LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var record = new LogRecord(
                DateTime.Now,
                new WriteContext()
                {
                    LoggerName = categoryName,
                    LogLevel = logLevel,
                    EventId = eventId,
                    State = state,
                    Exception = exception,
                    Formatter = (o, e) => formatter((TState)o, e),
                });
            _logs.Enqueue(record);
        }

        private class LogSinkLogger : ILogger
        {
            private readonly string _categoryName;
            private readonly LogSinkProvider _logSinkProvider;

            public LogSinkLogger(string categoryName, LogSinkProvider logSinkProvider)
            {
                _categoryName = categoryName;
                _logSinkProvider = logSinkProvider;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                _logSinkProvider.Log(_categoryName, logLevel, eventId, state, exception, formatter);
            }
        }
    }

}

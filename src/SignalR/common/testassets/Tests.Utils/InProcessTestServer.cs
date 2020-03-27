// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public abstract class InProcessTestServer : IDisposable
    {
        internal abstract event Action<LogRecord> ServerLogged;

        public abstract string WebSocketsUrl { get; }

        public abstract string Url { get; }

        public abstract void Dispose();
    }

    public class InProcessTestServer<TStartup> : InProcessTestServer
        where TStartup : class
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private IWebHost _host;
        private IHostApplicationLifetime _lifetime;
        private readonly IDisposable _logToken;
        private readonly IDisposable _extraDisposable;

        private readonly LogSinkProvider _logSinkProvider;
        private string _url;

        internal override event Action<LogRecord> ServerLogged
        {
            add => _logSinkProvider.RecordLogged += value;
            remove => _logSinkProvider.RecordLogged -= value;
        }

        public override string WebSocketsUrl => Url.Replace("http", "ws");

        public override string Url => _url;

        public static async Task<InProcessTestServer<TStartup>> StartServer(ILoggerFactory loggerFactory, IDisposable disposable = null)
        {
            var server = new InProcessTestServer<TStartup>(loggerFactory, disposable);
            await server.StartServerInner();
            return server;
        }

        private InProcessTestServer() : this(loggerFactory: null, null)
        {
        }

        private InProcessTestServer(ILoggerFactory loggerFactory, IDisposable disposable)
        {
            _extraDisposable = disposable;
            _logSinkProvider = new LogSinkProvider();

            if (loggerFactory == null)
            {
                var testLog = AssemblyTestLog.ForAssembly(typeof(TStartup).Assembly);
                _logToken = testLog.StartTestLog(null, $"{nameof(InProcessTestServer<TStartup>)}_{typeof(TStartup).Name}",
                    out _loggerFactory, nameof(InProcessTestServer));
            }
            else
            {
                _loggerFactory = loggerFactory;
            }

            _loggerFactory = new WrappingLoggerFactory(_loggerFactory);
            _loggerFactory.AddProvider(_logSinkProvider);
            _logger = _loggerFactory.CreateLogger<InProcessTestServer<TStartup>>();
        }

        private async Task StartServerInner()
        {
            // We're using 127.0.0.1 instead of localhost to ensure that we use IPV4 across different OSes
            var url = "http://127.0.0.1:0";

            _host = new WebHostBuilder()
                .ConfigureLogging(builder => builder
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddProvider(new ForwardingLoggerProvider(_loggerFactory)))
                .UseStartup(typeof(TStartup))
                .UseKestrel()
                .UseUrls(url)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .Build();

            _logger.LogInformation("Starting test server...");
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                await _host.StartAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                var logs = _logSinkProvider.GetLogs();
                throw new TimeoutException($"Timed out waiting for application to start.{Environment.NewLine}Startup Logs:{Environment.NewLine}{RenderLogs(logs)}");
            }

            _logger.LogInformation("Test Server started");

            // Get the URL from the server
            _url = _host.ServerFeatures.Get<IServerAddressesFeature>().Addresses.Single();

            _lifetime = _host.Services.GetRequiredService<IHostApplicationLifetime>();
            _lifetime.ApplicationStopped.Register(() =>
            {
                _logger.LogInformation("Test server shut down");
                _logToken?.Dispose();
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

        public override void Dispose()
        {
            _extraDisposable?.Dispose();
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
}

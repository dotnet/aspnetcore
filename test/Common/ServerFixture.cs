// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;


namespace Microsoft.AspNetCore.SignalR.Tests.Common
{
    public class ServerFixture<TStartup> : IDisposable
        where TStartup : class
    {
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;
        private IWebHost _host;
        private IApplicationLifetime _lifetime;
        private readonly IDisposable _logToken;

        public string BaseUrl => "http://localhost:3000";

        public string WebSocketsUrl => BaseUrl.Replace("http", "ws");

        public ServerFixture()
        {
            var testLog = AssemblyTestLog.ForAssembly(typeof(ServerFixture<TStartup>).Assembly);
            _logToken = testLog.StartTestLog(null, $"{nameof(ServerFixture<TStartup>)}_{typeof(TStartup).Name}" , out _loggerFactory, "ServerFixture");
            _logger = _loggerFactory.CreateLogger<ServerFixture<TStartup>>();

            StartServer();
        }

        private void StartServer()
        {
            _host = new WebHostBuilder()
                .ConfigureLogging(builder => builder.AddProvider(new ForwardingLoggerProvider(_loggerFactory)))
                .UseStartup(typeof(TStartup))
                .UseKestrel()
                .UseUrls(BaseUrl)
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

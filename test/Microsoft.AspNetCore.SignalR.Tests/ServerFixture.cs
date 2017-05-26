// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class ServerFixture : IDisposable
    {
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;
        private IWebHost host;
        private IApplicationLifetime lifetime;
        private readonly IDisposable _logToken;

        public string BaseUrl => "http://localhost:3000";

        public string WebSocketsUrl => BaseUrl.Replace("http", "ws");

        public ServerFixture()
        {
            var testLog = AssemblyTestLog.ForAssembly(typeof(ServerFixture).Assembly);
            _logToken = testLog.StartTestLog(null, typeof(ServerFixture).FullName, out _loggerFactory, "ServerFixture");
            _logger = _loggerFactory.CreateLogger<ServerFixture>();

            StartServer();
        }

        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddSockets();
                services.AddSignalR();
                services.AddEndPoint<EchoEndPoint>();
            }

            public void Configure(IApplicationBuilder app, IHostingEnvironment env)
            {
                app.UseSockets(options => options.MapEndPoint<EchoEndPoint>("echo"));
                app.UseSignalR(options => options.MapHub<UncreatableHub>("uncreatable"));
            }
        }

        private void StartServer()
        {
            host = new WebHostBuilder()
                .UseLoggerFactory(_loggerFactory)
                .UseKestrel()
                .UseUrls(BaseUrl)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .Build();

            var t = Task.Run(() => host.Start());
            _logger.LogInformation("Starting test server...");
            lifetime = host.Services.GetRequiredService<IApplicationLifetime>();
            if (!lifetime.ApplicationStarted.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)))
            {
                // t probably faulted
                if (t.IsFaulted)
                {
                    throw t.Exception.InnerException;
                }
                throw new TimeoutException("Timed out waiting for application to start.");
            }
            _logger.LogInformation("Test Server started");

            lifetime.ApplicationStopped.Register(() =>
            {
                _logger.LogInformation("Test server shut down");
                _logToken.Dispose();
            });
        }

        public void Dispose()
        {
            _logger.LogInformation("Shutting down test server");
            host.Dispose();
        }
    }

}

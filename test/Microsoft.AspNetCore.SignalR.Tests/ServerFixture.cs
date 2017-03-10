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

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class ServerFixture : IDisposable
    {
        private ILoggerFactory _loggerFactory;
        private IWebHost host;
        private IApplicationLifetime lifetime;

        public string BaseUrl => "http://localhost:3000";

        public string WebSocketsUrl => BaseUrl.Replace("http", "ws");

        public ServerFixture()
        {
            _loggerFactory = new LoggerFactory();

            var _verbose = string.Equals(Environment.GetEnvironmentVariable("SIGNALR_TESTS_VERBOSE"), "1");
            if (_verbose)
            {
                _loggerFactory.AddConsole(LogLevel.Debug);
            }
            if (Debugger.IsAttached)
            {
                _loggerFactory.AddDebug();
            }
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
                app.UseSockets(options => options.MapEndpoint<EchoEndPoint>("/echo"));
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
            Console.WriteLine("Starting test server...");
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
        }

        public void Dispose()
        {
            host.Dispose();
        }
    }

}

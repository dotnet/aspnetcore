// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Blazor.E2ETest.Infrastructure
{
    public class StaticServerFixture : IDisposable
    {
        private IWebHost _host;

        public string Start(string path)
        {
            _host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(path)
                .UseWebRoot(string.Empty)
                .UseStartup<Startup>()
                .UseUrls("http://127.0.0.1:0")
                .Build();
            StartWebHostInBackgroundThread(_host);

            return _host.ServerFeatures
                .Get<IServerAddressesFeature>()
                .Addresses.Single();
        }

        public void Dispose()
        {
            _host.StopAsync();
        }

        private static void StartWebHostInBackgroundThread(IWebHost host)
        {
            var serverStarted = new ManualResetEvent(false);

            new Thread(() =>
            {
                host.Start();
                serverStarted.Set();
            }).Start();

            serverStarted.WaitOne();
        }

        private class Startup
        {
            public void Configure(IApplicationBuilder app)
            {
                app.UseFileServer();
            }
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DevHostServerProgram = Microsoft.AspNetCore.Blazor.DevServer.Server.Program;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures
{
    public class DevHostServerFixture<TProgram> : WebHostServerFixture
    {
        public string Environment { get; set; }
        public string PathBase { get; set; }
        public string ContentRoot { get; private set; }

        protected override IWebHost CreateWebHost()
        {
            ContentRoot = FindSampleOrTestSitePath(
                typeof(TProgram).Assembly.FullName);

            var args = new List<string>
            {
                "--urls", "http://127.0.0.1:0",
                "--contentroot", ContentRoot,
                "--pathbase", PathBase,
                "--applicationpath", typeof(TProgram).Assembly.Location,
            };

            if (!string.IsNullOrEmpty(Environment))
            {
                args.Add("--environment");
                args.Add(Environment);
            }

            return new FakeWebHost(DevHostServerProgram.BuildWebHost(args.ToArray()));
        }

        private class FakeWebHost : IWebHost
        {
            private readonly IHost _realHost;

            public FakeWebHost(IHost realHost)
            {
                _realHost = realHost;
            }

            public IFeatureCollection ServerFeatures => ((IServer)_realHost.Services.GetService(typeof(IServer))).Features;

            public IServiceProvider Services => _realHost.Services;

            public void Dispose() => _realHost.Dispose();

            public void Start() => _realHost.Start();

            public Task StartAsync(CancellationToken cancellationToken = default) => _realHost.StartAsync();

            public Task StopAsync(CancellationToken cancellationToken = default) => _realHost.StopAsync();
        }
    }
}

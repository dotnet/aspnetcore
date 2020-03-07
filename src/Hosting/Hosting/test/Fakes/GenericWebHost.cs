// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting.Tests.Fakes
{
    internal class GenericWebHost : IWebHost
    {
        private readonly IHost _host;

        public GenericWebHost(IHost host)
        {
            _host = host;
        }

        public IFeatureCollection ServerFeatures => Services.GetRequiredService<IServer>().Features;

        public IServiceProvider Services => _host.Services;

        public void Dispose() => _host.Dispose();

        public void Start() => _host.Start();

        public Task StartAsync(CancellationToken cancellationToken = default) => _host.StartAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken = default) => _host.StopAsync(cancellationToken);
    }
}

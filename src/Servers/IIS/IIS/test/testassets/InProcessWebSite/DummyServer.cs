// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;

namespace TestSite
{
    public class DummyServer : IServer
    {
        public void Dispose()
        {
        }

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            return Task.Delay(TimeSpan.MaxValue);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.Delay(TimeSpan.MaxValue);
        }

        public IFeatureCollection Features { get; }
    }
}

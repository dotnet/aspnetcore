// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;

namespace TestSite;

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

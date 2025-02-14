// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting;

public class WebHostServiceTests
{
    // Reasonable timeout for our test operations to complete.
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    [ConditionalFact]
    public async Task StopBeforeServiceStarted()
    {
        var host = new WebHostBuilder().UseServer(new FakeServer()).Configure(x => { }).Build();
        var webHostService = new WebHostService(host);
        var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

        applicationLifetime.StopApplication();
        webHostService.Start();

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => Task.Delay(OperationTimeout, applicationLifetime.ApplicationStopped));
    }

    [ConditionalFact]
    public async Task StopAfterServiceStarted()
    {
        var host = new WebHostBuilder().UseServer(new FakeServer()).Configure(x => { }).Build();
        var webHostService = new WebHostService(host);
        var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

        webHostService.Start();
        applicationLifetime.StopApplication();

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => Task.Delay(OperationTimeout, applicationLifetime.ApplicationStopped));
    }

    private sealed class FakeServer : IServer
    {
        IFeatureCollection IServer.Features { get; }
        public RequestDelegate RequestDelegate { get; private set; }

        public void Dispose() { }

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            RequestDelegate = ctx => throw new NotSupportedException();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

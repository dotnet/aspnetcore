// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;

public abstract class WebHostServerFixture : ServerFixture, IAsyncDisposable, IAsyncLifetime
{
    protected override string StartAndGetRootUri()
    {
        Host = CreateWebHost();
        RunInBackgroundThread(Host.Start);
        return Host.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()
            .Addresses.Single();
    }

    public IHost Host { get; set; }

    public override void Dispose()
    {
        DisposeCore().AsTask().Wait();
    }

    protected abstract IHost CreateWebHost();
    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeCore().AsTask();

    ValueTask IAsyncDisposable.DisposeAsync() => DisposeCore();

    private async ValueTask DisposeCore()
    {
        // This can be null if creating the webhost throws, we don't want to throw here and hide
        // the original exception.
        Host?.Dispose();
        await Host?.StopAsync();
    }
}

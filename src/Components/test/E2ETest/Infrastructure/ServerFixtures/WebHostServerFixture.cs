// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;

public abstract class WebHostServerFixture : ServerFixture
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
        // This can be null if creating the webhost throws, we don't want to throw here and hide
        // the original exception.
        Host?.Dispose();
        Host?.StopAsync();
    }

    protected abstract IHost CreateWebHost();
}

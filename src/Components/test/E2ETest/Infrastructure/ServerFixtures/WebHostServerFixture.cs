// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Server;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures
{
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
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Threading;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;

namespace ServerComparison.TestSites
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            var builder = new WebHostBuilder()
                .UseServer(new NoopServer())
                .UseConfiguration(config)
                .UseStartup("Microsoft.AspNetCore.Hosting.TestSites");

            var host = builder.Build();

            host.Run();
        }
    }

    public class NoopServer : IServer
    {
        public void Dispose()
        {
        }

        public IFeatureCollection Features { get; } = new FeatureCollection();

        public void Start<TContext>(IHttpApplication<TContext> application)
        {
        }
    }
}


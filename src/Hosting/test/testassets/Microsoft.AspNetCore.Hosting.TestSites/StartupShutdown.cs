// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting.TestSites;

public class StartupShutdown
{
    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IHostApplicationLifetime lifetime)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            Console.WriteLine("Started");
        });
        lifetime.ApplicationStopping.Register(() =>
        {
            Console.WriteLine("Stopping firing");
            System.Threading.Thread.Sleep(200);
            Console.WriteLine("Stopping end");
        });
        lifetime.ApplicationStopped.Register(() =>
        {
            Console.WriteLine("Stopped firing");
            System.Threading.Thread.Sleep(200);
            Console.WriteLine("Stopped end");
        });

        app.Run(context =>
        {
            return context.Response.WriteAsync("Hello World");
        });
    }
}


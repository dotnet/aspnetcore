// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Kestrel;
using Microsoft.AspNet.Server.Kestrel.Https;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;

namespace SampleApp
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IApplicationEnvironment env)
        {
            var ksi = app.ServerFeatures.Get<IKestrelServerInformation>();
            //ksi.ThreadCount = 4;
            ksi.NoDelay = true;

            loggerFactory.MinimumLevel = LogLevel.Debug;

            loggerFactory.AddConsole(LogLevel.Debug);

            var testCertPath = Path.Combine(
                env.ApplicationBasePath,
                @"../../test/Microsoft.AspNet.Server.KestrelTests/TestResources/testCert.pfx");

            if (File.Exists(testCertPath))
            {
                app.UseKestrelHttps(new X509Certificate2(testCertPath, "testPassword"));
            }
            else
            {
                Console.WriteLine("Could not find certificate at '{0}'. HTTPS is not enabled.", testCertPath);
            }

            app.Run(async context =>
            {
                Console.WriteLine("{0} {1}{2}{3}",
                    context.Request.Method,
                    context.Request.PathBase,
                    context.Request.Path,
                    context.Request.QueryString);

                var connectionFeature = context.Connection;
                Console.WriteLine($"Peer: {connectionFeature.RemoteIpAddress?.ToString()} {connectionFeature.RemotePort}");
                Console.WriteLine($"Sock: {connectionFeature.LocalIpAddress?.ToString()} {connectionFeature.LocalPort}");
                Console.WriteLine($"IsLocal: {connectionFeature.IsLocal}");

                context.Response.ContentLength = 11;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Hello world");
            });
        }
    }
}

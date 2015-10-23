// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Kestrel;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Logging;

#if DNX451
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNet.Server.Kestrel.Https;
#endif

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

#if DNX451
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
#endif

            app.Run(async context =>
            {
                Console.WriteLine("{0} {1}{2}{3}",
                    context.Request.Method,
                    context.Request.PathBase,
                    context.Request.Path,
                    context.Request.QueryString);

                context.Response.ContentLength = 11;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Hello world");
            });
        }
    }
}

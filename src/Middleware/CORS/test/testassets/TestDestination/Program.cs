// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace SampleDestination
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using (var host = WebHost.CreateDefaultBuilder(args)
                .UseUrls("http://+:0")
                .UseStartup(GetStartupType())
                .Build())
            {
                host.Run();
            }
        }

        private static Type GetStartupType()
        {
            var startup = Environment.GetEnvironmentVariable("CORS_STARTUP");
            if (startup == null)
            {
                return typeof(Startup);
            }
            else
            {
                switch (startup)
                {
                    case "Startup":
                        return typeof(Startup);
                    case "StartupWithoutEndpointRouting":
                        return typeof(StartupWithoutEndpointRouting);
                }
            }

            throw new InvalidOperationException("Could not resolve the startup type. Unexpected CORS_STARTUP environment variable.");
        }
    }
}

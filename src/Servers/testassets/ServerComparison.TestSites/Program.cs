// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
                .UseConfiguration(config)
                .ConfigureLogging((_, factory) =>
                {
                    factory.AddConsole();
                    factory.AddFilter("Console", level => level >= LogLevel.Warning);
                })
                .UseStartup("ServerComparison.TestSites");

            // Switch between Kestrel, IIS, and HttpSys for different tests. Default to Kestrel for normal app execution.
            if (string.Equals(builder.GetSetting("server"), "Microsoft.AspNetCore.Server.HttpSys", StringComparison.Ordinal))
            {
                if (string.Equals(builder.GetSetting("environment") ??
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    "NtlmAuthentication", System.StringComparison.Ordinal))
                {
                    // Set up NTLM authentication for HttpSys as follows.
                    // For IIS and IISExpress use inetmgr to setup NTLM authentication on the application or
                    // modify the applicationHost.config to enable NTLM.
                    builder.UseHttpSys(options =>
                    {
                        options.Authentication.AllowAnonymous = true;
                        options.Authentication.Schemes =
                            AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM;
                    });
                }
                else
                {
                    builder.UseHttpSys();
                }
            }
            else
            {
                // Check that we are not using IIS inproc before we add Kestrel.
                builder.UseKestrel();
            }

            builder.UseIISIntegration();
            builder.UseIIS();

            var host = builder.Build();

            host.Run();
        }
    }
}


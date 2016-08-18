// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Server;

namespace ServerComparison.TestSites.Standalone
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
                .UseIISIntegration()
                .UseStartup("ServerComparison.TestSites.Standalone");

            // Switch between Kestrel and WebListener for different tests. Default to Kestrel for normal app execution.
            if (string.Equals(builder.GetSetting("server"), "Microsoft.AspNetCore.Server.WebListener", System.StringComparison.Ordinal))
            {
                if (string.Equals(builder.GetSetting("environment") ??
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    "NtlmAuthentication", System.StringComparison.Ordinal))
                {
                    // Set up NTLM authentication for WebListener as follows.
                    // For IIS and IISExpress use inetmgr to setup NTLM authentication on the application or
                    // modify the applicationHost.config to enable NTLM.
                    builder.UseWebListener(options =>
                    {
                        options.ListenerSettings.Authentication.AllowAnonymous = true;
                        options.ListenerSettings.Authentication.Schemes =
                            AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM;
                    });
                }
                else
                {
                    builder.UseWebListener();
                }
            }
            else
            {
                builder.UseKestrel();
            }

            var host = builder.Build();

            host.Run();
        }
    }
}


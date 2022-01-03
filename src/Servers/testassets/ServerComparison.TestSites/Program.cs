// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ServerComparison.TestSites;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = new WebHostBuilder()
            .UseConfiguration(new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build())
            .ConfigureLogging((_, factory) =>
            {
                factory.AddConsole();
                factory.AddFilter("Console", level => level >= LogLevel.Warning);
            })
            .UseStartup("ServerComparison.TestSites");

        builder.UseKestrel();
        builder.UseIISIntegration();
        builder.UseIIS();

        // Switch between Kestrel, IIS, and HttpSys for different tests. Default to Kestrel for normal app execution.
        if (string.Equals(builder.GetSetting("server"), "Microsoft.AspNetCore.Server.HttpSys", StringComparison.OrdinalIgnoreCase))
        {
            builder.UseHttpSys(options =>
            {
                if (string.Equals(builder.GetSetting("environment") ??
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    "NtlmAuthentication", StringComparison.OrdinalIgnoreCase))
                {
                    // Set up NTLM authentication for HttpSys as follows.
                    // For IIS and IISExpress use inetmgr to setup NTLM authentication on the application or
                    // modify the applicationHost.config to enable NTLM.
                    options.Authentication.AllowAnonymous = true;
                    options.Authentication.Schemes = AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM;
                }
            });
        }

        var host = builder.Build();

        host.Run();
    }
}


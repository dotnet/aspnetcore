// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Server;
using System;
using System.IO;
using System.Threading;

namespace AspnetCoreModule.TestSites.Standard
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();
            
            string startUpClassString = Environment.GetEnvironmentVariable("ANCMTestStartupClassName");
            IWebHostBuilder builder = null;
            if (!string.IsNullOrEmpty(startUpClassString))
            {
                if (startUpClassString == "StartupCompressionCaching")
                {
                    builder = new WebHostBuilder()
                        .UseConfiguration(config)
                        .UseIISIntegration()
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseStartup<StartupCompressionCaching>();
                }
                else if (startUpClassString == "StartupNoCompressionCaching")
                {
                    StartupCompressionCaching.CompressionMode = false;
                    builder = new WebHostBuilder()
                        .UseConfiguration(config)
                        .UseIISIntegration()
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseStartup<StartupCompressionCaching>();
                }
                else if (startUpClassString == "StartupHelloWorld")
                {
                    builder = new WebHostBuilder()
                        .UseConfiguration(config)
                        .UseIISIntegration()
                        .UseStartup<StartupHelloWorld>();
                }
                else if (startUpClassString == "StartupNtlmAuthentication")
                {
                    builder = new WebHostBuilder()
                        .UseConfiguration(config)
                        .UseIISIntegration()
                        .UseStartup<StartupNtlmAuthentication>();
                }
                else
                {
                    throw new System.Exception("Invalid startup class name : " + startUpClassString);
                }
            }
            else
            {
                builder = new WebHostBuilder()
                    .UseConfiguration(config)
                    .UseIISIntegration()
                    .UseStartup<Startup>();
            }

            string startupDelay = Environment.GetEnvironmentVariable("ANCMTestStartUpDelay");
            if (!string.IsNullOrEmpty(startupDelay))
            {
                Startup.SleeptimeWhileStarting = Convert.ToInt32(startupDelay);
            }

            if (Startup.SleeptimeWhileStarting != 0)
            {
                Thread.Sleep(Startup.SleeptimeWhileStarting);
            }

            string shutdownDelay = Environment.GetEnvironmentVariable("ANCMTestShutdownDelay");
            if (!string.IsNullOrEmpty(shutdownDelay))
            {
                Startup.SleeptimeWhileClosing = Convert.ToInt32(shutdownDelay);
            }
            
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
            if (Startup.SleeptimeWhileClosing != 0)
            {
                Thread.Sleep(Startup.SleeptimeWhileClosing);
            }
        }
    }
}


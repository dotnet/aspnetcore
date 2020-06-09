using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MusicStore
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .Build();

            var builder = new WebHostBuilder()
                .UseConfiguration(config)
                .UseIISIntegration()
                .UseStartup("MusicStore")
                .UseDefaultServiceProvider((context, options) => {
                    options.ValidateScopes = true;
                });

            var environment = builder.GetSetting("environment") ??
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (string.Equals(builder.GetSetting("server"), "Microsoft.AspNetCore.Server.HttpSys", System.StringComparison.Ordinal))
            {
                if (string.Equals(environment, "NtlmAuthentication", System.StringComparison.Ordinal))
                {
                    // Set up NTLM authentication for WebListener like below.
                    // For IIS and IISExpress: Use inetmgr to setup NTLM authentication on the application vDir or
                    // modify the applicationHost.config to enable NTLM.
                    builder.UseHttpSys(options =>
                    {
                        options.Authentication.Schemes = AuthenticationSchemes.NTLM;
                        options.Authentication.AllowAnonymous = false;
                    });
                }
                else
                {
                    builder.UseHttpSys();
                }
            }
            else
            {
                builder.UseKestrel();
            }

            // In Proc
            builder.UseIIS();

            builder.ConfigureLogging(factory =>
            {
                factory.AddConsole();

                var logLevel = string.Equals(environment, "Development", StringComparison.Ordinal) ? LogLevel.Information : LogLevel.Warning;
                factory.SetMinimumLevel(logLevel);

                // Turn off Info logging for EF commands
                factory.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
            });

            var host = builder.Build();

            host.Run();
        }
    }
}

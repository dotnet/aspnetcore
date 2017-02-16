using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Configuration;

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
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseConfiguration(config)
                .UseIISIntegration()
                .UseStartup("MusicStore");

            if (string.Equals(builder.GetSetting("server"), "Microsoft.AspNetCore.Server.HttpSys", System.StringComparison.Ordinal))
            {
                var environment = builder.GetSetting("environment") ??
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

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

            var host = builder.Build();

            host.Run();
        }
    }
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Server;

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
                // We set the server by name before default args so that command line arguments can override it.
                // This is used to allow deployers to choose the server for testing.
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .UseConfiguration(config)
                .UseIISIntegration()
                .UseStartup("MusicStore");

            if (string.Equals(builder.GetSetting("server"), "Microsoft.AspNetCore.Server.WebListener", System.StringComparison.Ordinal)
                && string.Equals(builder.GetSetting("environment"), "NtlmAuthentication", System.StringComparison.Ordinal))
            {
                if (string.Equals(builder.GetSetting("environment") ??
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                "NtlmAuthentication", System.StringComparison.Ordinal))
                {
                    options.Listener.AuthenticationManager.AuthenticationSchemes = AuthenticationSchemes.NTLM;
                });
            }

            var host = builder.Build();

            host.Run();
        }
    }
}

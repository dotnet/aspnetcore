using Microsoft.AspNetCore.Hosting;
using Microsoft.Net.Http.Server;

namespace MusicStore.Standalone
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = new WebHostBuilder()
                // We set the server by name before default args so that command line arguments can override it.
                // This is used to allow deployers to choose the server for testing.
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .UseDefaultHostingConfiguration(args)
                .UseIISIntegration()
                .UseStartup("MusicStore.Standalone");

            if (string.Equals(builder.GetSetting("server"), "Microsoft.AspNetCore.Server.WebListener", System.StringComparison.Ordinal)
                && string.Equals(builder.GetSetting("environment"), "NtlmAuthentication", System.StringComparison.Ordinal))
            {
                // Set up NTLM authentication for WebListener like below.
                // For IIS and IISExpress: Use inetmgr to setup NTLM authentication on the application vDir or
                // modify the applicationHost.config to enable NTLM.
                builder.UseWebListener(options =>
                {
                    options.Listener.AuthenticationManager.AuthenticationSchemes = AuthenticationSchemes.NTLM;
                });
            }

            var host = builder.Build();

            host.Run();
        }
    }
}

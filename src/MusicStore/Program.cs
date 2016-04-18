using Microsoft.AspNetCore.Hosting;
using Microsoft.Net.Http.Server;

namespace MusicStore
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = new WebHostBuilder()
                .UseDefaultHostingConfiguration(args)
                .UseIISIntegration()
                .UseStartup("MusicStore");

            // Switch beteween Kestrel and WebListener for different tests. Default to Kestrel for normal app execution.
            if (string.Equals(builder.GetSetting("server"), "Microsoft.AspNetCore.Server.WebListener", System.StringComparison.Ordinal))
            {
                if (string.Equals(builder.GetSetting("environment"), "NtlmAuthentication", System.StringComparison.Ordinal))
                {
                    // Set up NTLM authentication for WebListener as follows.
                    // For IIS and IISExpress use inetmgr to setup NTLM authentication on the application or
                    // modify the applicationHost.config to enable NTLM.
                    builder.UseWebListener(options =>
                    {
                        options.Listener.AuthenticationManager.AuthenticationSchemes = AuthenticationSchemes.NTLM;
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

using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutobahnTestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            var builder = new WebHostBuilder()
                .ConfigureLogging(loggingBuilder => loggingBuilder.AddConsole())
                .UseConfiguration(config)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>();

            if (string.Equals(builder.GetSetting("server"), "Microsoft.AspNetCore.Server.HttpSys", System.StringComparison.Ordinal))
            {
                Console.WriteLine("Using HttpSys server");
                builder.UseHttpSys();
            }
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_PORT")))
            {
                // ANCM is hosting the process.
                // The port will not yet be configured at this point, but will also not require HTTPS.
                Console.WriteLine("Detected ANCM, using Kestrel");
                builder.UseKestrel();
            }
            else
            {
                // Also check "server.urls" for back-compat.
                var urls = builder.GetSetting(WebHostDefaults.ServerUrlsKey) ?? builder.GetSetting("server.urls");
                builder.UseSetting(WebHostDefaults.ServerUrlsKey, string.Empty);

                Console.WriteLine($"Using Kestrel, URL: {urls}");

                if (urls.Contains(";"))
                {
                    throw new NotSupportedException("This test app does not support multiple endpoints.");
                }

                var uri = new Uri(urls);

                builder.UseKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, uri.Port, listenOptions =>
                    {
                        if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                        {
                            var certPath = Path.Combine(AppContext.BaseDirectory, "TestResources", "testCert.pfx");
                            Console.WriteLine($"Using SSL with certificate: {certPath}");
                            listenOptions.UseHttps(certPath, "testPassword");
                        }
                    });
                });
            }

            var host = builder.Build();
            host.Run();
        }
    }
}

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

            var loggerFactory = new LoggerFactory()
                .AddConsole();
            var logger = loggerFactory.CreateLogger<Program>();

            var builder = new WebHostBuilder()
                .UseLoggerFactory(loggerFactory)
                .UseConfiguration(config)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>();

            if (string.Equals(builder.GetSetting("server"), "Microsoft.AspNetCore.Server.HttpSys", System.StringComparison.Ordinal))
            {
                logger.LogInformation("Using HttpSys server");
                builder.UseHttpSys();
            }
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_PORT")))
            {
                // ANCM is hosting the process.
                // The port will not yet be configured at this point, but will also not require HTTPS.
                logger.LogInformation("Detected ANCM, using Kestrel");
                builder.UseKestrel();
            }
            else
            {
                // Also check "server.urls" for back-compat.
                var urls = builder.GetSetting(WebHostDefaults.ServerUrlsKey) ?? builder.GetSetting("server.urls");
                builder.UseSetting(WebHostDefaults.ServerUrlsKey, string.Empty);

                logger.LogInformation("Using Kestrel, URL: {url}", urls);

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
                            logger.LogInformation("Using SSL with certificate: {certPath}", certPath);
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

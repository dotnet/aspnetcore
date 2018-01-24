using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Http2SampleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppContext.SetSwitch("Switch.Microsoft.AspNetCore.Server.Kestrel.Experimental.Http2", isEnabled: true);

            var hostBuilder = new WebHostBuilder()
                .ConfigureLogging((_, factory) =>
                {
                    // Set logging to the MAX.
                    factory.SetMinimumLevel(LogLevel.Trace);
                    factory.AddConsole();
                })
                .UseKestrel((context, options) =>
                {
                    var basePort = context.Configuration.GetValue<int?>("BASE_PORT") ?? 5000;

                    // Run callbacks on the transport thread
                    options.ApplicationSchedulingMode = SchedulingMode.Inline;

                    options.Listen(IPAddress.Any, basePort, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        listenOptions.UseHttps("testCert.pfx", "testPassword");
                        listenOptions.UseConnectionLogging();
                    });
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>();

            hostBuilder.Build().Run();
        }
    }
}

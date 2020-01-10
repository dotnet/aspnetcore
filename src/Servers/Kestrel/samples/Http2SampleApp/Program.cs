using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Http2SampleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var hostBuilder = new WebHostBuilder()
                .ConfigureLogging((_, factory) =>
                {
                    // Set logging to the MAX.
                    factory.SetMinimumLevel(LogLevel.Trace);
                    factory.AddConsole();
                })
                .UseKestrel()
                .ConfigureKestrel((context, options) =>
                {
                    var basePort = context.Configuration.GetValue<int?>("BASE_PORT") ?? 5000;

                    // Http/1.1 endpoint for comparison
                    options.ListenAnyIP(basePort, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1;
                    });

                    // TLS Http/1.1 or HTTP/2 endpoint negotiated via ALPN
                    options.ListenAnyIP(basePort + 1, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        listenOptions.UseHttps();
                        listenOptions.Use((context, next) =>
                        {
                            // https://tools.ietf.org/html/rfc7540#appendix-A
                            // Allows filtering TLS handshakes on a per connection basis

                            var tlsFeature = context.Features.Get<ITlsHandshakeFeature>();

                            if (tlsFeature.CipherAlgorithm == CipherAlgorithmType.Null)
                            {
                                throw new NotSupportedException("Prohibited cipher: " + tlsFeature.CipherAlgorithm);
                            }

                            return next();
                        });
                    });

                    // Prior knowledge, no TLS handshake. WARNING: Not supported by browsers
                    // but useful for the h2spec tests
                    options.ListenAnyIP(basePort + 5, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                    });
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>();

            hostBuilder.Build().Run();
        }
    }
}

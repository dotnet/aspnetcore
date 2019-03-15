using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
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

                    // Run callbacks on the transport thread
                    options.ApplicationSchedulingMode = SchedulingMode.Inline;

                    // Http/1.1 endpoint for comparison
                    options.Listen(IPAddress.Any, basePort, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1;
                    });

                    // TLS Http/1.1 or HTTP/2 endpoint negotiated via ALPN
                    options.Listen(IPAddress.Any, basePort + 1, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        listenOptions.UseHttps();
                        listenOptions.ConnectionAdapters.Add(new TlsFilterAdapter());
                    });

                    // Prior knowledge, no TLS handshake. WARNING: Not supported by browsers
                    // but useful for the h2spec tests
                    options.Listen(IPAddress.Any, basePort + 5, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                    });
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>();

            hostBuilder.Build().Run();
        }

        // https://tools.ietf.org/html/rfc7540#appendix-A
        // Allows filtering TLS handshakes on a per connection basis
        private class TlsFilterAdapter : IConnectionAdapter
        {
            public bool IsHttps => false;

            public Task<IAdaptedConnection> OnConnectionAsync(ConnectionAdapterContext context)
            {
                var tlsFeature = context.Features.Get<ITlsHandshakeFeature>();

                if (tlsFeature.CipherAlgorithm == CipherAlgorithmType.Null)
                {
                    throw new NotSupportedException("Prohibited cipher: " + tlsFeature.CipherAlgorithm);
                }

                return Task.FromResult<IAdaptedConnection>(new AdaptedConnection(context.ConnectionStream));
            }

            private class AdaptedConnection : IAdaptedConnection
            {
                public AdaptedConnection(Stream adaptedStream)
                {
                    ConnectionStream = adaptedStream;
                }

                public Stream ConnectionStream { get; }

                public void Dispose()
                {
                }
            }
        }
    }
}

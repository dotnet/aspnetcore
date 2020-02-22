using System;
using System.Buffers;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace QuicSampleApp
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.Run((httpContext) =>
            {
                return Task.CompletedTask;
            });
        }

        public static void Main(string[] args)
        {
            var hostBuilder = new WebHostBuilder()
                 .ConfigureLogging((_, factory) =>
                 {
                     factory.SetMinimumLevel(LogLevel.Debug);
                     factory.AddConsole();
                 })
                 .UseKestrel()
                 .UseQuic(options =>
                 {
                     options.Certificate = null;
                     options.RegistrationName = "AspNetCore-MsQuic";
                     options.Alpn = "QuicTest";
                     options.IdleTimeout = TimeSpan.FromHours(1);
                 })
                 .ConfigureKestrel((context, options) =>
                 {
                     var basePort = 5555;

                     options.Listen(IPAddress.Any, basePort, listenOptions =>
                     {
                         listenOptions.Protocols = HttpProtocols.Http3;
                         listenOptions.Use((next) =>
                         {
                             return async connection =>
                             {
                                 var streamFeature = connection.Features.Get<IQuicStreamListenerFeature>();
                                 if (streamFeature != null)
                                 {
                                     while (true)
                                     {
                                         var connectionContext = await streamFeature.AcceptAsync();
                                         if (connectionContext == null)
                                         {
                                             return;
                                         }
                                         _ = next(connectionContext);
                                     }
                                 }
                                 else
                                 {
                                     await next(connection);
                                 }
                             };
                         });

                         async Task EchoServer(ConnectionContext connection)
                         {
                             // For graceful shutdown
                             try
                             {
                                 while (true)
                                 {
                                     var result = await connection.Transport.Input.ReadAsync();

                                     if (result.IsCompleted)
                                     {
                                         break;
                                     }

                                     await connection.Transport.Output.WriteAsync(result.Buffer.ToArray());

                                     connection.Transport.Input.AdvanceTo(result.Buffer.End);
                                 }
                             }
                             catch (OperationCanceledException)
                             {
                             }
                         }
                         listenOptions.Run(EchoServer);
                     });
                 })
                 .UseStartup<Startup>();

            hostBuilder.Build().Run();
        }
    }
}

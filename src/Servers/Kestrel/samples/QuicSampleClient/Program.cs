using System;
using System.Buffers;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic;
using Microsoft.AspNetCore.Connections.Abstractions.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace QuicSampleClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
               .ConfigureLogging(loggingBuilder =>
               {
                   loggingBuilder.AddConsole();
                   loggingBuilder.SetMinimumLevel(LogLevel.Error);
               })
               .ConfigureServices(services =>
               {
                   services.AddSingleton<IConnectionFactory, MsQuicConnectionFactory>();
                   services.AddSingleton<MsQuicClientService>();
                   services.AddOptions<MsQuicTransportOptions>();
                   services.Configure<MsQuicTransportOptions>((options) =>
                   {
                       options.Alpn = "QuicTest";
                       options.RegistrationName = "Quic-AspNetCore-client";
                       options.Certificate = CertificateLoader.LoadFromStoreCert("localhost", StoreName.My.ToString(), StoreLocation.CurrentUser, true);
                       options.IdleTimeout = TimeSpan.FromHours(1);
                   });
               })
               .Build();
            await host.Services.GetService<MsQuicClientService>().RunAsync();
        }

        private class MsQuicClientService
        {
            private readonly IConnectionFactory _connectionFactory;
            private readonly ILogger<MsQuicClientService> _logger;
            public MsQuicClientService(IConnectionFactory connectionFactory, ILogger<MsQuicClientService> logger)
            {
                _connectionFactory = connectionFactory;
                _logger = logger;
            }

            public async Task RunAsync()
            {
                var connectionContext = await _connectionFactory.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 5555));
                var createStreamFeature = connectionContext.Features.Get<IQuicCreateStreamFeature>();
                var streamContext = await createStreamFeature.StartBidirectionalStreamAsync();

                Console.CancelKeyPress += new ConsoleCancelEventHandler((sender, args) =>
                {
                    streamContext.Transport.Input.CancelPendingRead();
                    streamContext.Transport.Output.CancelPendingFlush();
                });

                while (true)
                {
                    try
                    {
                        var input = Console.ReadLine();
                        if (input.Length == 0)
                        {
                            continue;
                        }
                        var flushResult = await streamContext.Transport.Output.WriteAsync(Encoding.ASCII.GetBytes(input));
                        if (flushResult.IsCanceled)
                        {
                            break;
                        }

                        var readResult = await streamContext.Transport.Input.ReadAsync();
                        if (readResult.IsCanceled)
                        {
                            break;
                        }
                        if (readResult.Buffer.Length > 0)
                        {
                            Console.WriteLine(Encoding.ASCII.GetString(readResult.Buffer.ToArray()));
                        }

                        streamContext.Transport.Input.AdvanceTo(readResult.Buffer.End);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        break;
                    }
                }

                await streamContext.Transport.Input.CompleteAsync();
                await streamContext.Transport.Output.CompleteAsync();
            }
        }
    }
}

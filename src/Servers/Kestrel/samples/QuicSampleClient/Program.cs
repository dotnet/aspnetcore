using System;
using System.Buffers;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Experimental.Quic;
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
                   services.AddSingleton<IMultiplexedConnectionFactory, QuicConnectionFactory>();
                   services.AddSingleton<QuicClientService>();
                   services.AddOptions<QuicTransportOptions>();
                   services.Configure<QuicTransportOptions>((options) =>
                   {
                       options.Alpn = "QuicTest";
                       options.Certificate = null;
                       options.IdleTimeout = TimeSpan.FromHours(1);
                   });
               })
               .Build();
            await host.Services.GetService<QuicClientService>().RunAsync();
        }

        private class QuicClientService
        {
            private readonly IMultiplexedConnectionFactory _connectionFactory;
            private readonly ILogger<QuicClientService> _logger;
            public QuicClientService(IMultiplexedConnectionFactory connectionFactory, ILogger<QuicClientService> logger)
            {
                _connectionFactory = connectionFactory;
                _logger = logger;
            }

            public async Task RunAsync()
            {
                Console.WriteLine("Starting");
                var connectionContext = await _connectionFactory.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 5555));
                var streamContext = await connectionContext.ConnectAsync();

                Console.CancelKeyPress += new ConsoleCancelEventHandler((sender, args) =>
                {
                    streamContext.Transport.Input.CancelPendingRead();
                    streamContext.Transport.Output.CancelPendingFlush();
                });

                var input = "asdf";
                while (true)
                {
                    try
                    {
                        //var input = Console.ReadLine();
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
                        if (readResult.IsCanceled || readResult.IsCompleted)
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

using System;
using System.Buffers;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic;
using Microsoft.AspNetCore.Connections.Abstractions.Features;

namespace QuicSampleClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var transportOptions = new MsQuicTransportOptions()
            {
                Alpn = "QuicTest",
                RegistrationName = "Quic-AspNetCore-client",
                Certificate = CertificateLoader.LoadFromStoreCert("localhost", StoreName.My.ToString(), StoreLocation.CurrentUser, true),
                IdleTimeout = TimeSpan.FromHours(1),
            };

            var transportContext = new MsQuicTransportContext(null, null, transportOptions);
            var factory = new MsQuicConnectionFactory(transportContext);
            var connectionContext = await factory.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 5555));
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

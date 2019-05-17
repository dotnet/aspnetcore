// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

namespace PlaintextApp
{
    public class Startup
    {
        private static readonly byte[] _helloWorldBytes = Encoding.UTF8.GetBytes("Hello, World!");

        public void Configure(IApplicationBuilder app)
        {
            app.Run((httpContext) =>
            {
                var payload = _helloWorldBytes;
                var response = httpContext.Response;

                response.StatusCode = 200;
                response.ContentType = "text/plain";
                response.ContentLength = payload.Length;

                return response.BodyWriter.WriteAsync(payload).GetAsTask();
            });
        }

        public static async Task Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, 5001);
                })
                // .UseLibuv()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .Build();

            var hostTask = host.RunAsync();
            // var serverTask = ServerAsync(new SocketTransportFactory(), 5002);
            // var serverTask2 = ServerAsync(new LibuvTransportFactory(), 5003);

            await hostTask;
            // await serverTask;
            // await serverTask2;
        }

        private static async Task ServerAsync(IConnectionListenerFactory factory, int port)
        {
            await using var listener = await factory.BindAsync(new IPEndPoint(IPAddress.Loopback, port));

            while (true)
            {
                var connection = await listener.AcceptAsync();

                // Fire and forget so we can handle more than a single connection at a time
                _ = HandleConnectionAsync(connection);

                static async Task HandleConnectionAsync(ConnectionContext connection)
                {
                    await using (connection)
                    {
                        while (true)
                        {
                            var result = await connection.Transport.Input.ReadAsync();
                            var buffer = result.Buffer;

                            foreach (var segment in buffer)
                            {
                                await connection.Transport.Output.WriteAsync(segment);
                            }

                            if (result.IsCompleted)
                            {
                                break;
                            }

                            connection.Transport.Input.AdvanceTo(buffer.End);
                        }
                    }
                }
            }
        }
    }

    internal static class ValueTaskExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task GetAsTask(this in ValueTask<FlushResult> valueTask)
        {
            if (valueTask.IsCompletedSuccessfully)
            {
                // Signal consumption to the IValueTaskSource
                valueTask.GetAwaiter().GetResult();
                return Task.CompletedTask;
            }
            else
            {
                return valueTask.AsTask();
            }
        }
    }
}

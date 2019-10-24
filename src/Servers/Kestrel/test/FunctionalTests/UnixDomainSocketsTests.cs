// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class UnixDomainSocketsTest : TestApplicationErrorLoggerLoggedTest
    {
#if LIBUV
        [OSSkipCondition(OperatingSystems.Windows, SkipReason = "Libuv does not support unix domain sockets on Windows.")]
#else
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_RS4)]
#endif
        [SkipOnHelix("https://github.com/aspnet/AspNetCore/issues/14382", Queues = "Windows.10.Amd64.Open")]
        [ConditionalFact]
        [CollectDump]
        public async Task TestUnixDomainSocket()
        {
            var path = Path.GetTempFileName();

            Delete(path);

            try
            {
                var serverConnectionCompletedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                async Task EchoServer(ConnectionContext connection)
                {
                    // For graceful shutdown
                    var notificationFeature = connection.Features.Get<IConnectionLifetimeNotificationFeature>();

                    try
                    {
                        while (true)
                        {
                            var result = await connection.Transport.Input.ReadAsync(notificationFeature.ConnectionClosedRequested);

                            if (result.IsCompleted)
                            {
                                Logger.LogDebug("Application receive loop ending for connection {connectionId}.", connection.ConnectionId);
                                break;
                            }

                            await connection.Transport.Output.WriteAsync(result.Buffer.ToArray());

                            connection.Transport.Input.AdvanceTo(result.Buffer.End);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.LogDebug("Graceful shutdown triggered for {connectionId}.", connection.ConnectionId);
                    }
                    finally
                    {
                        serverConnectionCompletedTcs.TrySetResult(null);
                    }
                }

                var hostBuilder = TransportSelector.GetWebHostBuilder()
                    .UseKestrel(o =>
                    {
                        o.ListenUnixSocket(path, builder =>
                        {
                            builder.Run(EchoServer);
                        });
                    })
                    .ConfigureServices(AddTestLogging)
                    .Configure(c => { });

                using (var host = hostBuilder.Build())
                {
                    await host.StartAsync().DefaultTimeout();

                    using (var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
                    {
                        await socket.ConnectAsync(new UnixDomainSocketEndPoint(path)).DefaultTimeout();

                        var data = Encoding.ASCII.GetBytes("Hello World");
                        await socket.SendAsync(data, SocketFlags.None).DefaultTimeout();

                        var buffer = new byte[data.Length];
                        var read = 0;
                        while (read < data.Length)
                        {
                            read += await socket.ReceiveAsync(buffer.AsMemory(read, buffer.Length - read), SocketFlags.None).DefaultTimeout();
                        }

                        Assert.Equal(data, buffer);
                    }

                    // Wait for the server to complete the loop because of the FIN
                    await serverConnectionCompletedTcs.Task.DefaultTimeout();

                    await host.StopAsync().DefaultTimeout();
                }
            }
            finally
            {
                Delete(path);
            }
        }

        private static void Delete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (FileNotFoundException)
            {

            }
        }
    }
}

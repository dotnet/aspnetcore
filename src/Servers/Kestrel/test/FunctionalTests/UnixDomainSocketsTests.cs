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
using Microsoft.AspNetCore.Http;
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

#if LIBUV
        [OSSkipCondition(OperatingSystems.Windows, SkipReason = "Libuv does not support unix domain sockets on Windows.")]
#else
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win8, WindowsVersions.Win81, WindowsVersions.Win2008R2, SkipReason = "UnixDomainSocketEndPoint is not supported on older versions of Windows")]
#endif
        [ConditionalFact]
        [CollectDump]
        public async Task TestUnixDomainSocketWithUrl()
        {
            var path = Path.GetTempFileName();
            var url = $"http://unix:/{path}";

            Delete(path);

            try
            {
                var hostBuilder = TransportSelector.GetWebHostBuilder()
                    .UseUrls(url)
                    .UseKestrel()
                    .ConfigureServices(AddTestLogging)
                    .Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            var buffer = new byte[(int)context.Request.ContentLength];

                            await context.Request.Body.ReadAsync(buffer);
                            await context.Response.WriteAsync(Encoding.ASCII.GetString(buffer));
                        });
                    });

                using (var host = hostBuilder.Build())
                {
                    await host.StartAsync();

                    using (var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
                    {
                        await socket.ConnectAsync(new UnixDomainSocketEndPoint(path));

                        var data = $"Hello World {Path.GetRandomFileName()}";

                        StringBuilder httpRequest = new StringBuilder();
                        httpRequest.AppendLine($"POST / HTTP/1.1");
                        httpRequest.AppendLine($"Host: localhost");
                        httpRequest.AppendLine($"Content-Length: {data.Length}");
                        httpRequest.AppendLine();
                        httpRequest.AppendLine(data);

                        // https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.networkstream.dataavailable?view=netcore-3.0
                        using (var stream = new NetworkStream(socket))
                        {
                            var requestData = Encoding.UTF8.GetBytes(httpRequest.ToString());
                            stream.Write(requestData, 0, requestData.Length);

                            if (stream.CanRead)
                            {
                                byte[] myReadBuffer = new byte[1024];
                                StringBuilder myCompleteMessage = new StringBuilder();
                                int numberOfBytesRead = 0;

                                // Incoming message may be larger than the buffer size.
                                do
                                {
                                    numberOfBytesRead = stream.Read(myReadBuffer, 0, myReadBuffer.Length);
                                    myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(myReadBuffer, 0, numberOfBytesRead));
                                }
                                while (stream.DataAvailable);

                                // https://github.com/dotnet/corefx/issues/5999
                                // .NET Core HttpClient does not support unix sockets, it's difficult to parse raw response data. below is a little hacky way.
                                Assert.True(myCompleteMessage.ToString().IndexOf(data) > -1);
                            }
                            else
                            {
                                Assert.True(false, "Network stream cannot be read");
                            }
                        }
                    }
                    await host.StopAsync();
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

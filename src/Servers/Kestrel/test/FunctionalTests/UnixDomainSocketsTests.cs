// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Globalization;
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
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

#if SOCKETS
namespace Microsoft.AspNetCore.Server.Kestrel.Sockets.FunctionalTests;
#else
namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
#endif

public class UnixDomainSocketsTest : TestApplicationErrorLoggerLoggedTest
{
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_RS4)]
    [ConditionalFact]
    [CollectDump]
    public async Task TestUnixDomainSocket()
    {
        var path = Path.GetTempFileName();

        Delete(path);

        try
        {
            var serverConnectionCompletedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

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
                    serverConnectionCompletedTcs.TrySetResult();
                }
            }

            var hostBuilder = TransportSelector.GetHostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseKestrel(o =>
                        {
                            o.ListenUnixSocket(path, builder =>
                            {
                                builder.Run(EchoServer);
                            });
                        })
                        .Configure(c => { });
                })
                .ConfigureServices(AddTestLogging);

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
                        var bytesReceived = await socket.ReceiveAsync(buffer.AsMemory(read, buffer.Length - read), SocketFlags.None).DefaultTimeout();
                        read += bytesReceived;
                        if (bytesReceived <= 0)
                        {
                            break;
                        }
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

    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_RS4)]
    [ConditionalFact]
    [CollectDump]
    public async Task TestUnixDomainSocketWithUrl()
    {
        var path = Path.GetTempFileName();
        var url = $"http://unix:/{path}";

        Delete(path);

        try
        {
            var hostBuilder = TransportSelector.GetHostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseUrls(url)
                        .UseKestrel()
                        .Configure(app =>
                        {
                            app.Run(async context =>
                            {
                                await context.Response.WriteAsync("Hello World");
                            });
                        });
                })
                .ConfigureServices(AddTestLogging);

            using (var host = hostBuilder.Build())
            {
                await host.StartAsync().DefaultTimeout();

                // https://github.com/dotnet/corefx/issues/5999
                // .NET Core HttpClient does not support unix sockets, it's difficult to parse raw response data. below is a little hacky way.
                using (var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
                {
                    await socket.ConnectAsync(new UnixDomainSocketEndPoint(path)).DefaultTimeout();

                    var httpRequest = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost:\r\nConnection: close\r\n\r\n");
                    await socket.SendAsync(httpRequest, SocketFlags.None).DefaultTimeout();

                    var readBuffer = new byte[512];
                    var read = 0;
                    while (true)
                    {
                        var bytesReceived = await socket.ReceiveAsync(readBuffer.AsMemory(read), SocketFlags.None).DefaultTimeout();
                        read += bytesReceived;
                        if (bytesReceived <= 0)
                        {
                            break;
                        }
                    }

                    var httpResponse = Encoding.ASCII.GetString(readBuffer, 0, read);
                    int httpStatusStart = httpResponse.IndexOf(' ') + 1;
                    Assert.False(httpStatusStart == 0, $"Space not found in '{httpResponse}'.");
                    int httpStatusEnd = httpResponse.IndexOf(' ', httpStatusStart);
                    Assert.False(httpStatusEnd == -1, $"Second space not found in '{httpResponse}'.");

                    var httpStatus = int.Parse(httpResponse.Substring(httpStatusStart, httpStatusEnd - httpStatusStart), CultureInfo.InvariantCulture);
                    Assert.Equal(StatusCodes.Status200OK, httpStatus);

                }
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

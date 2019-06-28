// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
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
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win8, WindowsVersions.Win81, WindowsVersions.Win2008R2, SkipReason = "UnixDomainSocketEndPoint is not supported on older versions of Windows")]
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
                    try
                    {
                        while (true)
                        {
                            var result = await connection.Transport.Input.ReadAsync();

                            if (result.IsCompleted)
                            {
                                Logger.LogDebug("Application receive loop ending for connection {connectionId}.", connection.ConnectionId);
                                break;
                            }

                            await connection.Transport.Output.WriteAsync(result.Buffer.ToArray());

                            connection.Transport.Input.AdvanceTo(result.Buffer.End);
                        }
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
                    await host.StartAsync();

                    var factory = new SocketConnectionFactory(LoggerFactory);
                    var endPoint = new UnixDomainSocketEndPoint(path);
                    await using (var connection = await factory.ConnectAsync(endPoint))
                    {
                        var data = Encoding.ASCII.GetBytes("Hello World");
                        await connection.Transport.Output.WriteAsync(data);

                        var buffer = await connection.Transport.Input.ReadAsync(data.Length);
                        Assert.Equal(data, buffer);
                    }

                    // Wait for the server to complete the loop because of the FIN
                    await serverConnectionCompletedTcs.Task.DefaultTimeout();

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

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests.TestHelpers;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests
{
    public class LibuvTransportTests
    {
        public static IEnumerable<object[]> OneToTen => Enumerable.Range(1, 10).Select(i => new object[] { i });

        [Fact]
        public async Task TransportCanBindAndStop()
        {
            var transportContext = new TestLibuvTransportContext();
            var transport = new LibuvConnectionListener(transportContext, new IPEndPoint(IPAddress.Loopback, 0));

            // The transport can no longer start threads without binding to an endpoint.
            await transport.BindAsync();
            await transport.DisposeAsync();
        }

        [Fact]
        public async Task TransportCanBindUnbindAndStop()
        {
            var transportContext = new TestLibuvTransportContext();
            var transport = new LibuvConnectionListener(transportContext, new IPEndPoint(IPAddress.Loopback, 0));

            await transport.BindAsync();
            await transport.StopAsync();
            await transport.DisposeAsync();
        }

        [Fact]
        public async Task ConnectionCanReadAndWrite()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 0);

            var transportContext = new TestLibuvTransportContext();
            var transport = new LibuvConnectionListener(transportContext, endpoint);

            await transport.BindAsync();
            endpoint = (IPEndPoint)transport.EndPoint;

            async Task EchoServerAsync()
            {
                await using var connection = await transport.AcceptAsync();

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

            var serverTask = EchoServerAsync();

            using (var socket = TestConnection.CreateConnectedLoopbackSocket(endpoint.Port))
            {
                var data = Encoding.ASCII.GetBytes("Hello World");
                socket.Send(data);

                var buffer = new byte[data.Length];
                var read = 0;
                while (read < data.Length)
                {
                    read += socket.Receive(buffer, read, buffer.Length - read, SocketFlags.None);
                }

                Assert.Equal(data, buffer);
            }

            await serverTask.DefaultTimeout();

            await transport.StopAsync();
            await transport.DisposeAsync();
        }

        [Fact]
        public async Task UnacceptedConnectionsAreAborted()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 0);

            var transportContext = new TestLibuvTransportContext();
            var transport = new LibuvConnectionListener(transportContext, endpoint);

            await transport.BindAsync();
            endpoint = (IPEndPoint)transport.EndPoint;

            async Task ConnectAsync()
            {
                using (var socket = TestConnection.CreateConnectedLoopbackSocket(endpoint.Port))
                {
                    var read = await socket.ReceiveAsync(new byte[10], SocketFlags.None);
                    Assert.Equal(0, read);
                }
            }

            var connectTask = ConnectAsync();

            await transport.StopAsync();
            await transport.DisposeAsync();

            // The connection was accepted because libuv eagerly accepts connections
            // they sit in a queue in each listener, we want to make sure that resources
            // are cleaned up if they are never accepted by the caller

            await connectTask.DefaultTimeout();
        }

        [ConditionalTheory]
        [MemberData(nameof(OneToTen))]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Tests fail on OS X due to low file descriptor limit.")]
        public async Task OneToTenThreads(int threadCount)
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
            var serviceContext = new TestServiceContext();
            var testApplication = new DummyApplication(context =>
            {
                return context.Response.WriteAsync("Hello World");
            });

            listenOptions.UseHttpServer(listenOptions.ConnectionAdapters, serviceContext, testApplication, HttpProtocols.Http1);

            var transportContext = new TestLibuvTransportContext
            {
                Options = new LibuvTransportOptions { ThreadCount = threadCount }
            };

            var transport = new LibuvConnectionListener(transportContext, listenOptions.EndPoint);
            await transport.BindAsync();
            listenOptions.EndPoint = transport.EndPoint;

            var dispatcher = new ConnectionDispatcher(serviceContext, listenOptions.Build());
            var acceptTask = dispatcher.StartAcceptingConnections(transport);

            using (var client = new HttpClient())
            {
                // Send 20 requests just to make sure we don't get any failures
                var requestTasks = new List<Task<string>>();
                for (int i = 0; i < 20; i++)
                {
                    var requestTask = client.GetStringAsync($"http://127.0.0.1:{listenOptions.IPEndPoint.Port}/");
                    requestTasks.Add(requestTask);
                }

                foreach (var result in await Task.WhenAll(requestTasks))
                {
                    Assert.Equal("Hello World", result);
                }
            }

            await transport.StopAsync().ConfigureAwait(false);

            await acceptTask.ConfigureAwait(false);

            if (!await serviceContext.ConnectionManager.CloseAllConnectionsAsync(default).ConfigureAwait(false))
            {
                await serviceContext.ConnectionManager.AbortAllConnectionsAsync().ConfigureAwait(false);
            }

            await transport.DisposeAsync().ConfigureAwait(false);
        }
    }
}

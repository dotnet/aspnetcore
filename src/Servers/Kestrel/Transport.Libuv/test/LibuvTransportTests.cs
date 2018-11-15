// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests.TestHelpers;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests
{
    public class LibuvTransportTests
    {
        public static TheoryData<ListenOptions> ConnectionAdapterData => new TheoryData<ListenOptions>
        {
            new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)),
            new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new PassThroughConnectionAdapter() }
            }
        };

        public static IEnumerable<object[]> OneToTen => Enumerable.Range(1, 10).Select(i => new object[] { i });

        [Fact]
        public async Task TransportCanBindAndStop()
        {
            var transportContext = new TestLibuvTransportContext();
            var transport = new LibuvTransport(transportContext,
                new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)));

            // The transport can no longer start threads without binding to an endpoint.
            await transport.BindAsync();
            await transport.StopAsync();
        }

        [Fact]
        public async Task TransportCanBindUnbindAndStop()
        {
            var transportContext = new TestLibuvTransportContext();
            var transport = new LibuvTransport(transportContext, new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)));

            await transport.BindAsync();
            await transport.UnbindAsync();
            await transport.StopAsync();
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ConnectionCanReadAndWrite(ListenOptions listenOptions)
        {
            var serviceContext = new TestServiceContext();
            listenOptions.UseHttpServer(listenOptions.ConnectionAdapters, serviceContext, new DummyApplication(TestApp.EchoApp), HttpProtocols.Http1);

            var transportContext = new TestLibuvTransportContext
            {
                ConnectionDispatcher = new ConnectionDispatcher(serviceContext, listenOptions.Build())
            };

            var transport = new LibuvTransport(transportContext, listenOptions);

            await transport.BindAsync();

            using (var socket = TestConnection.CreateConnectedLoopbackSocket(listenOptions.IPEndPoint.Port))
            {
                var data = "Hello World";
                socket.Send(Encoding.ASCII.GetBytes($"POST / HTTP/1.0\r\nContent-Length: 11\r\n\r\n{data}"));
                var buffer = new byte[data.Length];
                var read = 0;
                while (read < data.Length)
                {
                    read += socket.Receive(buffer, read, buffer.Length - read, SocketFlags.None);
                }
            }

            Assert.True(await serviceContext.ConnectionManager.CloseAllConnectionsAsync(new CancellationTokenSource(TestConstants.DefaultTimeout).Token));
            await transport.UnbindAsync();
            await transport.StopAsync();
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
                ConnectionDispatcher = new ConnectionDispatcher(serviceContext, listenOptions.Build()),
                Options = new LibuvTransportOptions { ThreadCount = threadCount }
            };

            var transport = new LibuvTransport(transportContext, listenOptions);

            await transport.BindAsync();

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

            await transport.UnbindAsync();

            if (!await serviceContext.ConnectionManager.CloseAllConnectionsAsync(default).ConfigureAwait(false))
            {
                await serviceContext.ConnectionManager.AbortAllConnectionsAsync().ConfigureAwait(false);
            }

            await transport.StopAsync();
        }
    }
}

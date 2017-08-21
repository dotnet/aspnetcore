// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests.TestHelpers;
using Microsoft.AspNetCore.Testing;
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

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task TransportCanBindUnbindAndStop(ListenOptions listenOptions)
        {
            var transportContext = new TestLibuvTransportContext();
            var transport = new LibuvTransport(transportContext, listenOptions);

            await transport.BindAsync();
            await transport.UnbindAsync();
            await transport.StopAsync();
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ConnectionCanReadAndWrite(ListenOptions listenOptions)
        {
            var serviceContext = new TestServiceContext();
            listenOptions.UseHttpServer(listenOptions.ConnectionAdapters, serviceContext, new DummyApplication(TestApp.EchoApp));

            var transportContext = new TestLibuvTransportContext()
            {
                ConnectionHandler = new ConnectionHandler(serviceContext, listenOptions.Build())
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

            await transport.UnbindAsync();
            await transport.StopAsync();
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.AspNetCore.Server.KestrelTests.TestHelpers;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class ConnectionTests
    {
        [Fact]
        public async Task DoesNotEndConnectionOnZeroRead()
        {
            using (var mockConnectionHandler = new MockConnectionHandler())
            {
                var mockLibuv = new MockLibuv();
                var serviceContext = new TestServiceContext();
                serviceContext.TransportContext.ConnectionHandler = mockConnectionHandler;

                var transport = new LibuvTransport(mockLibuv, serviceContext.TransportContext, null);
                var thread = new LibuvThread(transport);

                try
                {
                    await thread.StartAsync();
                    await thread.PostAsync(_ =>
                    {
                        var listenerContext = new ListenerContext(serviceContext.TransportContext)
                        {
                            Thread = thread
                        };
                        var socket = new MockSocket(mockLibuv, Thread.CurrentThread.ManagedThreadId, serviceContext.TransportContext.Log);
                        var connection = new LibuvConnection(listenerContext, socket);
                        connection.Start();

                        LibuvFunctions.uv_buf_t ignored;
                        mockLibuv.AllocCallback(socket.InternalGetHandle(), 2048, out ignored);
                        mockLibuv.ReadCallback(socket.InternalGetHandle(), 0, ref ignored);
                    }, (object)null);

                    var readAwaitable = await mockConnectionHandler.Input.Reader.ReadAsync();
                    Assert.False(readAwaitable.IsCompleted);
                }
                finally
                {
                    await thread.StopAsync(TimeSpan.FromSeconds(1));
                }
            }
        }
    }
}
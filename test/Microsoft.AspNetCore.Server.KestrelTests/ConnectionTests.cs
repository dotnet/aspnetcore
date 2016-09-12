// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;
using Microsoft.AspNetCore.Server.KestrelTests.TestHelpers;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class ConnectionTests
    {
        [Fact]
        public async Task DoesNotEndConnectionOnZeroRead()
        {
            var mockLibuv = new MockLibuv();

            using (var engine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                engine.Start(count: 1);

                var trace = new TestKestrelTrace();
                var context = new ListenerContext(new TestServiceContext())
                {
                    FrameFactory = connectionContext => new Frame<HttpContext>(
                        new DummyApplication(httpContext => TaskCache.CompletedTask), connectionContext),
                    ServerAddress = ServerAddress.FromUrl("http://127.0.0.1:0"),
                    Thread = engine.Threads[0]
                };

                Connection connection = null;
                await context.Thread.PostAsync(_ =>
                {
                    var socket = new MockSocket(mockLibuv, Thread.CurrentThread.ManagedThreadId, trace);
                    connection = new Connection(context, socket);
                    connection.Start();

                    Libuv.uv_buf_t ignored;
                    mockLibuv.AllocCallback(socket.InternalGetHandle(), 2048, out ignored);
                    mockLibuv.ReadCallback(socket.InternalGetHandle(), 0, ref ignored);
                    Assert.False(connection.SocketInput.CheckFinOrThrow());
                }, null);

                connection.ConnectionControl.End(ProduceEndType.SocketDisconnect);
            }
        }
    }
}

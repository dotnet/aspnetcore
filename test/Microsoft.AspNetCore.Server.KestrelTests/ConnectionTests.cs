// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
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
            var serviceContext = new TestServiceContext
            {
                FrameFactory = connectionContext => new Frame<HttpContext>(
                    new DummyApplication(httpContext => TaskCache.CompletedTask), connectionContext)
            };

            // Ensure ProcessRequestAsync runs inline with the ReadCallback
            serviceContext.ThreadPool = new InlineLoggingThreadPool(serviceContext.Log);

            using (var engine = new KestrelEngine(mockLibuv, serviceContext))
            {
                engine.Start(count: 1);

                var listenerContext = new ListenerContext(serviceContext)
                {
                    ListenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)),
                    Thread = engine.Threads[0]
                };

                Connection connection = null;
                await listenerContext.Thread.PostAsync(_ =>
                {
                    var socket = new MockSocket(mockLibuv, Thread.CurrentThread.ManagedThreadId, serviceContext.Log);
                    connection = new Connection(listenerContext, socket);
                    connection.Start();

                    Libuv.uv_buf_t ignored;
                    mockLibuv.AllocCallback(socket.InternalGetHandle(), 2048, out ignored);
                    // This runs the ProcessRequestAsync inline
                    mockLibuv.ReadCallback(socket.InternalGetHandle(), 0, ref ignored);

                    var readAwaitable = connection.Input.Reader.ReadAsync();

                    Assert.False(readAwaitable.IsCompleted);
                }, (object)null);
                connection.ConnectionControl.End(ProduceEndType.SocketDisconnect);
            }
        }
    }
}
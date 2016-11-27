// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests
{
    public class HttpConnectionDispatcherTests
    {
        [Fact]
        public async Task GetIdReservesConnectionIdAndReturnsIt()
        {
            var lifetime = new ApplicationLifetime();
            var manager = new ConnectionManager(lifetime);
            using (var factory = new PipelineFactory())
            {
                var dispatcher = new HttpConnectionDispatcher(manager, factory, loggerFactory: null);
                var context = new DefaultHttpContext();
                var ms = new MemoryStream();
                context.Request.Path = "/getid";
                context.Response.Body = ms;
                await dispatcher.ExecuteAsync<TestEndPoint>("", context);

                var id = Encoding.UTF8.GetString(ms.ToArray());

                ConnectionState state;
                Assert.True(manager.TryGetConnection(id, out state));
                Assert.Equal(id, state.Connection.ConnectionId);
            }
        }

        [Fact]
        public async Task SendingToReservedConnectionsThatHaveNotConnectedThrows()
        {
            var lifetime = new ApplicationLifetime();
            var manager = new ConnectionManager(lifetime);
            var state = manager.ReserveConnection();

            using (var factory = new PipelineFactory())
            {
                var dispatcher = new HttpConnectionDispatcher(manager, factory, loggerFactory: null);
                var context = new DefaultHttpContext();
                context.Request.Path = "/send";
                var values = new Dictionary<string, StringValues>();
                values["id"] = state.Connection.ConnectionId;
                var qs = new QueryCollection(values);
                context.Request.Query = qs;
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await dispatcher.ExecuteAsync<TestEndPoint>("", context);
                });
            }
        }

        [Fact]
        public async Task SendingToUnknownConnectionIdThrows()
        {
            var lifetime = new ApplicationLifetime();
            var manager = new ConnectionManager(lifetime);
            using (var factory = new PipelineFactory())
            {
                var dispatcher = new HttpConnectionDispatcher(manager, factory, loggerFactory: null);
                var context = new DefaultHttpContext();
                context.Request.Path = "/send";
                var values = new Dictionary<string, StringValues>();
                values["id"] = "unknown";
                var qs = new QueryCollection(values);
                context.Request.Query = qs;
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await dispatcher.ExecuteAsync<TestEndPoint>("", context);
                });
            }
        }

        [Fact]
        public async Task SendingWithoutConnectionIdThrows()
        {
            var lifetime = new ApplicationLifetime();
            var manager = new ConnectionManager(lifetime);
            using (var factory = new PipelineFactory())
            {
                var dispatcher = new HttpConnectionDispatcher(manager, factory, loggerFactory: null);
                var context = new DefaultHttpContext();
                context.Request.Path = "/send";
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await dispatcher.ExecuteAsync<TestEndPoint>("", context);
                });
            }
        }
    }

    public class TestEndPoint : EndPoint
    {
        public override Task OnConnectedAsync(Connection connection)
        {
            throw new NotImplementedException();
        }
    }
}

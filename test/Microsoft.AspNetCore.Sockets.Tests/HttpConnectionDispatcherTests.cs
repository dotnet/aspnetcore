// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Channels;
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
            var manager = new ConnectionManager();
            using (var factory = new ChannelFactory())
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
            var manager = new ConnectionManager();
            var state = manager.ReserveConnection();

            using (var factory = new ChannelFactory())
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
            var manager = new ConnectionManager();
            using (var factory = new ChannelFactory())
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
            var manager = new ConnectionManager();
            using (var factory = new ChannelFactory())
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

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests
{
    public class HttpConnectionDispatcherTests
    {
        [Fact]
        public async Task GetIdReservesConnectionIdAndReturnsIt()
        {
            using (var factory = new PipelineFactory())
            {
                var manager = new ConnectionManager(factory);
                var dispatcher = new HttpConnectionDispatcher(manager, factory, new LoggerFactory());
                var context = new DefaultHttpContext();
                var services = new ServiceCollection();
                services.AddSingleton<TestEndPoint>();
                context.RequestServices = services.BuildServiceProvider();
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

        // REVIEW: No longer relevant since we establish the connection right away.
        //[Fact]
        //public async Task SendingToReservedConnectionsThatHaveNotConnectedThrows()
        //{
        //    using (var factory = new PipelineFactory())
        //    {
        //        var manager = new ConnectionManager(factory);
        //        var state = manager.ReserveConnection();

        //        var dispatcher = new HttpConnectionDispatcher(manager, factory, loggerFactory: null);
        //        var context = new DefaultHttpContext();
        //        context.Request.Path = "/send";
        //        var values = new Dictionary<string, StringValues>();
        //        values["id"] = state.Connection.ConnectionId;
        //        var qs = new QueryCollection(values);
        //        context.Request.Query = qs;
        //        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        //        {
        //            await dispatcher.ExecuteAsync<TestEndPoint>("", context);
        //        });
        //    }
        //}

        [Fact]
        public async Task SendingToUnknownConnectionIdThrows()
        {
            using (var factory = new PipelineFactory())
            {
                var manager = new ConnectionManager(factory);
                var dispatcher = new HttpConnectionDispatcher(manager, factory, new LoggerFactory());
                var context = new DefaultHttpContext();
                var services = new ServiceCollection();
                services.AddSingleton<TestEndPoint>();
                context.RequestServices = services.BuildServiceProvider();
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
            using (var factory = new PipelineFactory())
            {
                var manager = new ConnectionManager(factory);
                var dispatcher = new HttpConnectionDispatcher(manager, factory, new LoggerFactory());
                var context = new DefaultHttpContext();
                var services = new ServiceCollection();
                services.AddSingleton<TestEndPoint>();
                context.RequestServices = services.BuildServiceProvider();
                context.Request.Path = "/send";
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await dispatcher.ExecuteAsync<TestEndPoint>("", context);
                });
            }
        }
    }

    public class TestEndPoint : StreamingEndPoint
    {
        public override Task OnConnectedAsync(StreamingConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}

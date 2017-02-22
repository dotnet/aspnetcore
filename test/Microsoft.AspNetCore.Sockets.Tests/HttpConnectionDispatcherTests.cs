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
        public async Task NegotiateReservesConnectionIdAndReturnsIt()
        {
            var manager = CreateConnectionManager();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddSingleton<TestEndPoint>();
            context.RequestServices = services.BuildServiceProvider();
            var ms = new MemoryStream();
            context.Request.Path = "/negotiate";
            context.Response.Body = ms;
            await dispatcher.ExecuteAsync<TestEndPoint>("", context);

            var id = Encoding.UTF8.GetString(ms.ToArray());

            ConnectionState state;
            Assert.True(manager.TryGetConnection(id, out state));
            Assert.Equal(id, state.Connection.ConnectionId);
        }

        [Theory]
        [InlineData("/send")]
        [InlineData("/sse")]
        [InlineData("/poll")]
        [InlineData("/ws")]
        public async Task EndpointsThatAcceptConnectionId404WhenUnknownConnectionIdProvided(string path)
        {
            var manager = CreateConnectionManager();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            using (var strm = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                context.Response.Body = strm;

                var services = new ServiceCollection();
                services.AddSingleton<TestEndPoint>();
                context.RequestServices = services.BuildServiceProvider();
                context.Request.Path = path;
                var values = new Dictionary<string, StringValues>();
                values["id"] = "unknown";
                var qs = new QueryCollection(values);
                context.Request.Query = qs;

                await dispatcher.ExecuteAsync<TestEndPoint>("", context);

                Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
                await strm.FlushAsync();
                Assert.Equal("No Connection with that ID", Encoding.UTF8.GetString(strm.ToArray()));
            }
        }

        [Theory]
        [InlineData("/send")]
        [InlineData("/sse")]
        [InlineData("/poll")]
        public async Task EndpointsThatRequireConnectionId400WhenNoConnectionIdProvided(string path)
        {
            var manager = CreateConnectionManager();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());
            using (var strm = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                context.Response.Body = strm;
                var services = new ServiceCollection();
                services.AddSingleton<TestEndPoint>();
                context.RequestServices = services.BuildServiceProvider();
                context.Request.Path = path;

                await dispatcher.ExecuteAsync<TestEndPoint>("", context);

                Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
                await strm.FlushAsync();
                Assert.Equal("Connection ID required", Encoding.UTF8.GetString(strm.ToArray()));
            }
        }

        [Fact]
        public async Task CompletedEndPointEndsConnection()
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context = MakeRequest<ImmediatelyCompleteEndPoint>("/sse", state);

            await dispatcher.ExecuteAsync<ImmediatelyCompleteEndPoint>("", context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

            ConnectionState removed;
            bool exists = manager.TryGetConnection(state.Connection.ConnectionId, out removed);
            Assert.False(exists);
        }

        [Fact]
        public async Task SynchronusExceptionEndsConnection()
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context = MakeRequest<SynchronusExceptionEndPoint>("/sse", state);

            await dispatcher.ExecuteAsync<SynchronusExceptionEndPoint>("", context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

            ConnectionState removed;
            bool exists = manager.TryGetConnection(state.Connection.ConnectionId, out removed);
            Assert.False(exists);
        }

        [Fact]
        public async Task SynchronusExceptionEndsLongPollingConnection()
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context = MakeRequest<SynchronusExceptionEndPoint>("/poll", state);

            await dispatcher.ExecuteAsync<SynchronusExceptionEndPoint>("", context);

            Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);

            ConnectionState removed;
            bool exists = manager.TryGetConnection(state.Connection.ConnectionId, out removed);
            Assert.False(exists);
        }

        [Fact]
        public async Task CompletedEndPointEndsLongPollingConnection()
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context = MakeRequest<ImmediatelyCompleteEndPoint>("/poll", state);

            await dispatcher.ExecuteAsync<ImmediatelyCompleteEndPoint>("", context);

            Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);

            ConnectionState removed;
            bool exists = manager.TryGetConnection(state.Connection.ConnectionId, out removed);
            Assert.False(exists);
        }

        [Fact]
        public async Task RequestToActiveConnectionId409ForStreamingTransports()
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context1 = MakeRequest<TestEndPoint>("/sse", state);
            var context2 = MakeRequest<TestEndPoint>("/sse", state);

            var request1 = dispatcher.ExecuteAsync<TestEndPoint>("", context1);

            await dispatcher.ExecuteAsync<TestEndPoint>("", context2);

            Assert.Equal(StatusCodes.Status409Conflict, context2.Response.StatusCode);

            manager.CloseConnections();

            await request1;
        }

        [Fact]
        public async Task RequestToActiveConnectionIdKillsPreviousConnectionLongPolling()
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context1 = MakeRequest<TestEndPoint>("/poll", state);
            var context2 = MakeRequest<TestEndPoint>("/poll", state);

            var request1 = dispatcher.ExecuteAsync<TestEndPoint>("", context1);
            var request2 = dispatcher.ExecuteAsync<TestEndPoint>("", context2);

            await request1;

            Assert.Equal(StatusCodes.Status204NoContent, context1.Response.StatusCode);
            Assert.Equal(ConnectionState.ConnectionStatus.Active, state.Status);

            Assert.False(request2.IsCompleted);

            manager.CloseConnections();

            await request2;
        }

        [Theory]
        [InlineData("/sse")]
        [InlineData("/poll")]
        public async Task RequestToDisposedConnectionIdReturns404(string path)
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();
            state.Status = ConnectionState.ConnectionStatus.Disposed;

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context = MakeRequest<TestEndPoint>(path, state);

            await dispatcher.ExecuteAsync<TestEndPoint>("", context);

            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        }

        [Fact]
        public async Task ConnectionStateSetToInactiveAfterPoll()
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context = MakeRequest<TestEndPoint>("/poll", state);

            var task = dispatcher.ExecuteAsync<TestEndPoint>("", context);

            var buffer = Encoding.UTF8.GetBytes("Hello World");

            // Write to the transport so the poll yields
            await state.Connection.Transport.Output.WriteAsync(new Message(buffer, MessageType.Text, endOfMessage: true));

            await task;

            Assert.Equal(ConnectionState.ConnectionStatus.Inactive, state.Status);
            Assert.Null(state.RequestId);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public async Task BlockingConnectionWorksWithStreamingConnections()
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context = MakeRequest<BlockingEndPoint>("/sse", state);

            var task = dispatcher.ExecuteAsync<BlockingEndPoint>("", context);

            var buffer = Encoding.UTF8.GetBytes("Hello World");

            // Write to the application
            await state.Application.Output.WriteAsync(new Message(buffer, MessageType.Text, endOfMessage: true));

            await task;

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            ConnectionState removed;
            bool exists = manager.TryGetConnection(state.Connection.ConnectionId, out removed);
            Assert.False(exists);
        }

        [Fact]
        public async Task BlockingConnectionWorksWithLongPollingConnection()
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context = MakeRequest<BlockingEndPoint>("/poll", state);

            var task = dispatcher.ExecuteAsync<BlockingEndPoint>("", context);

            var buffer = Encoding.UTF8.GetBytes("Hello World");

            // Write to the application
            await state.Application.Output.WriteAsync(new Message(buffer, MessageType.Text, endOfMessage: true));

            await task;

            Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
            ConnectionState removed;
            bool exists = manager.TryGetConnection(state.Connection.ConnectionId, out removed);
            Assert.False(exists);
        }

        private static DefaultHttpContext MakeRequest<TEndPoint>(string path, ConnectionState state) where TEndPoint : EndPoint
        {
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddSingleton<TEndPoint>();
            context.RequestServices = services.BuildServiceProvider();
            context.Request.Path = path;
            var values = new Dictionary<string, StringValues>();
            values["id"] = state.Connection.ConnectionId;
            var qs = new QueryCollection(values);
            context.Request.Query = qs;
            return context;
        }

        private static ConnectionManager CreateConnectionManager()
        {
            return new ConnectionManager(new Logger<ConnectionManager>(new LoggerFactory()));
        }
    }

    public class BlockingEndPoint : EndPoint
    {
        public override Task OnConnectedAsync(Connection connection)
        {
            connection.Transport.Input.WaitToReadAsync().Wait();
            return Task.CompletedTask;
        }
    }

    public class SynchronusExceptionEndPoint : EndPoint
    {
        public override Task OnConnectedAsync(Connection connection)
        {
            throw new InvalidOperationException();
        }
    }

    public class ImmediatelyCompleteEndPoint : EndPoint
    {
        public override Task OnConnectedAsync(Connection connection)
        {
            return Task.CompletedTask;
        }
    }

    public class TestEndPoint : EndPoint
    {
        public override async Task OnConnectedAsync(Connection connection)
        {
            while (await connection.Transport.Input.WaitToReadAsync())
            {
            }
        }
    }
}

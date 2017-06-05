// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.SignalR.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests
{
    public class HttpConnectionDispatcherTests
    {
        // Redefined from MessageFormatter because we want constants to go in the Attributes
        private const string TextContentType = "application/vnd.microsoft.aspnetcore.endpoint-messages.v1+text";
        private const string BinaryContentType = "application/vnd.microsoft.aspnetcore.endpoint-messages.v1+binary";

        [Fact]
        public async Task NegotiateReservesConnectionIdAndReturnsIt()
        {
            var manager = CreateConnectionManager();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddEndPoint<TestEndPoint>();
            services.AddOptions();
            var ms = new MemoryStream();
            context.Request.Path = "/foo";
            context.Request.Method = "OPTIONS";
            context.Response.Body = ms;
            var builder = new SocketBuilder(services.BuildServiceProvider());
            builder.UseEndPoint<TestEndPoint>();
            var app = builder.Build();
            await dispatcher.ExecuteAsync(context, new HttpSocketOptions(), app);

            var id = Encoding.UTF8.GetString(ms.ToArray());

            Assert.True(manager.TryGetConnection(id, out var connection));
            Assert.Equal(id, connection.ConnectionId);
        }

        [Theory]
        [InlineData(TransportType.WebSockets)]
        [InlineData(TransportType.ServerSentEvents)]
        [InlineData(TransportType.LongPolling)]
        public async Task EndpointsThatAcceptConnectionId404WhenUnknownConnectionIdProvided(TransportType transportType)
        {
            var manager = CreateConnectionManager();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            using (var strm = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                context.Response.Body = strm;

                var services = new ServiceCollection();
                services.AddEndPoint<TestEndPoint>();
                services.AddOptions();
                context.Request.Path = "/foo";
                context.Request.Method = "GET";
                var values = new Dictionary<string, StringValues>();
                values["id"] = "unknown";
                var qs = new QueryCollection(values);
                context.Request.Query = qs;
                SetTransport(context, transportType);

                var builder = new SocketBuilder(services.BuildServiceProvider());
                builder.UseEndPoint<TestEndPoint>();
                var app = builder.Build();
                await dispatcher.ExecuteAsync(context, new HttpSocketOptions(), app);

                Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
                await strm.FlushAsync();
                Assert.Equal("No Connection with that ID", Encoding.UTF8.GetString(strm.ToArray()));
            }
        }


        [Fact]
        public async Task EndpointsThatAcceptConnectionId404WhenUnknownConnectionIdProvidedForPost()
        {
            var manager = CreateConnectionManager();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            using (var strm = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                context.Response.Body = strm;

                var services = new ServiceCollection();
                services.AddEndPoint<TestEndPoint>();
                services.AddOptions();
                context.Request.Path = "/foo";
                context.Request.Method = "POST";
                var values = new Dictionary<string, StringValues>();
                values["id"] = "unknown";
                var qs = new QueryCollection(values);
                context.Request.Query = qs;

                var builder = new SocketBuilder(services.BuildServiceProvider());
                builder.UseEndPoint<TestEndPoint>();
                var app = builder.Build();
                await dispatcher.ExecuteAsync(context, new HttpSocketOptions(), app);

                Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
                await strm.FlushAsync();
                Assert.Equal("No Connection with that ID", Encoding.UTF8.GetString(strm.ToArray()));
            }
        }

        [Theory]
        [InlineData(TransportType.ServerSentEvents)]
        [InlineData(TransportType.LongPolling)]
        public async Task EndpointsThatRequireConnectionId400WhenNoConnectionIdProvided(TransportType transportType)
        {
            var manager = CreateConnectionManager();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());
            using (var strm = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                context.Response.Body = strm;
                var services = new ServiceCollection();
                services.AddOptions();
                services.AddEndPoint<TestEndPoint>();
                context.Request.Path = "/foo";
                context.Request.Method = "GET";

                SetTransport(context, transportType);

                var builder = new SocketBuilder(services.BuildServiceProvider());
                builder.UseEndPoint<TestEndPoint>();
                var app = builder.Build();
                await dispatcher.ExecuteAsync(context, new HttpSocketOptions(), app);

                Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
                await strm.FlushAsync();
                Assert.Equal("Connection ID required", Encoding.UTF8.GetString(strm.ToArray()));
            }
        }

        [Fact]
        public async Task EndpointsThatRequireConnectionId400WhenNoConnectionIdProvidedForPost()
        {
            var manager = CreateConnectionManager();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());
            using (var strm = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                context.Response.Body = strm;
                var services = new ServiceCollection();
                services.AddOptions();
                services.AddEndPoint<TestEndPoint>();
                context.Request.Path = "/foo";
                context.Request.Method = "POST";

                var builder = new SocketBuilder(services.BuildServiceProvider());
                builder.UseEndPoint<TestEndPoint>();
                var app = builder.Build();
                await dispatcher.ExecuteAsync(context, new HttpSocketOptions(), app);

                Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
                await strm.FlushAsync();
                Assert.Equal("Connection ID required", Encoding.UTF8.GetString(strm.ToArray()));
            }
        }

        [Theory]
        [InlineData(TransportType.LongPolling, 204)]
        [InlineData(TransportType.WebSockets, 404)]
        [InlineData(TransportType.ServerSentEvents, 404)]
        public async Task EndPointThatOnlySupportsLongPollingRejectsOtherTransports(TransportType transportType, int status)
        {
            await CheckTransportSupported(TransportType.LongPolling, transportType, status);
        }

        [Theory]
        [InlineData(TransportType.ServerSentEvents, 200)]
        [InlineData(TransportType.WebSockets, 404)]
        [InlineData(TransportType.LongPolling, 404)]
        public async Task EndPointThatOnlySupportsSSERejectsOtherTransports(TransportType transportType, int status)
        {
            await CheckTransportSupported(TransportType.ServerSentEvents, transportType, status);
        }

        [Theory]
        [InlineData(TransportType.WebSockets, 200)]
        [InlineData(TransportType.ServerSentEvents, 404)]
        [InlineData(TransportType.LongPolling, 404)]
        public async Task EndPointThatOnlySupportsWebSockesRejectsOtherTransports(TransportType transportType, int status)
        {
            await CheckTransportSupported(TransportType.WebSockets, transportType, status);
        }

        [Theory]
        [InlineData(TransportType.LongPolling, 404)]
        public async Task EndPointThatOnlySupportsWebSocketsAndSSERejectsLongPolling(TransportType transportType, int status)
        {
            await CheckTransportSupported(TransportType.WebSockets | TransportType.ServerSentEvents, transportType, status);
        }

        [Fact]
        public async Task CompletedEndPointEndsConnection()
        {
            var manager = CreateConnectionManager();
            var connection = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context = MakeRequest("/foo", connection);
            SetTransport(context, TransportType.ServerSentEvents);

            var services = new ServiceCollection();
            services.AddEndPoint<ImmediatelyCompleteEndPoint>();
            var builder = new SocketBuilder(services.BuildServiceProvider());
            builder.UseEndPoint<ImmediatelyCompleteEndPoint>();
            var app = builder.Build();
            await dispatcher.ExecuteAsync(context, new HttpSocketOptions(), app);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

            bool exists = manager.TryGetConnection(connection.ConnectionId, out _);
            Assert.False(exists);
        }

        [Fact]
        public async Task SynchronusExceptionEndsConnection()
        {
            var manager = CreateConnectionManager();
            var connection = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());
            var context = MakeRequest("/foo", connection);
            SetTransport(context, TransportType.ServerSentEvents);

            var services = new ServiceCollection();
            services.AddEndPoint<SynchronusExceptionEndPoint>();
            var builder = new SocketBuilder(services.BuildServiceProvider());
            builder.UseEndPoint<SynchronusExceptionEndPoint>();
            var app = builder.Build();
            await dispatcher.ExecuteAsync(context, new HttpSocketOptions(), app);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

            bool exists = manager.TryGetConnection(connection.ConnectionId, out _);
            Assert.False(exists);
        }

        [Fact]
        public async Task CompletedEndPointEndsLongPollingConnection()
        {
            var manager = CreateConnectionManager();
            var connection = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context = MakeRequest("/foo", connection);

            var services = new ServiceCollection();
            services.AddEndPoint<ImmediatelyCompleteEndPoint>();
            var builder = new SocketBuilder(services.BuildServiceProvider());
            builder.UseEndPoint<ImmediatelyCompleteEndPoint>();
            var app = builder.Build();
            await dispatcher.ExecuteAsync(context, new HttpSocketOptions(), app);

            Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);

            bool exists = manager.TryGetConnection(connection.ConnectionId, out _);
            Assert.False(exists);
        }

        [Fact]
        public async Task WebSocketTransportTimesOutWhenCloseFrameNotReceived()
        {
            var manager = CreateConnectionManager();
            var connection = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context = MakeRequest("/foo", connection);
            SetTransport(context, TransportType.WebSockets);

            var services = new ServiceCollection();
            services.AddEndPoint<ImmediatelyCompleteEndPoint>();
            var builder = new SocketBuilder(services.BuildServiceProvider());
            builder.UseEndPoint<ImmediatelyCompleteEndPoint>();
            var app = builder.Build();
            var options = new HttpSocketOptions();
            options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(1);

            var task = dispatcher.ExecuteAsync(context, options, app);

            await task.OrTimeout();
        }

        [Theory]
        [InlineData(TransportType.WebSockets)]
        [InlineData(TransportType.ServerSentEvents)]
        public async Task RequestToActiveConnectionId409ForStreamingTransports(TransportType transportType)
        {
            var manager = CreateConnectionManager();
            var connection = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context1 = MakeRequest("/foo", connection);
            var context2 = MakeRequest("/foo", connection);

            SetTransport(context1, transportType);
            SetTransport(context2, transportType);

            var services = new ServiceCollection();
            services.AddEndPoint<TestEndPoint>();
            var builder = new SocketBuilder(services.BuildServiceProvider());
            builder.UseEndPoint<TestEndPoint>();
            var app = builder.Build();
            var options = new HttpSocketOptions();
            var request1 = dispatcher.ExecuteAsync(context1, options, app);

            await dispatcher.ExecuteAsync(context2, options, app);

            Assert.Equal(StatusCodes.Status409Conflict, context2.Response.StatusCode);

            var webSocketTask = Task.CompletedTask;

            var ws = (TestWebSocketConnectionFeature)context1.Features.Get<IHttpWebSocketFeature>();
            if (ws != null)
            {
                await ws.Client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }

            manager.CloseConnections();

            await request1.OrTimeout();
        }

        [Fact]
        public async Task RequestToActiveConnectionIdKillsPreviousConnectionLongPolling()
        {
            var manager = CreateConnectionManager();
            var connection = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context1 = MakeRequest("/foo", connection);
            var context2 = MakeRequest("/foo", connection);

            var services = new ServiceCollection();
            services.AddEndPoint<TestEndPoint>();
            var builder = new SocketBuilder(services.BuildServiceProvider());
            builder.UseEndPoint<TestEndPoint>();
            var app = builder.Build();
            var options = new HttpSocketOptions();
            var request1 = dispatcher.ExecuteAsync(context1, options, app);
            var request2 = dispatcher.ExecuteAsync(context2, options, app);

            await request1;

            Assert.Equal(StatusCodes.Status204NoContent, context1.Response.StatusCode);
            Assert.Equal(DefaultConnectionContext.ConnectionStatus.Active, connection.Status);

            Assert.False(request2.IsCompleted);

            manager.CloseConnections();

            await request2;
        }

        [Theory]
        [InlineData(TransportType.ServerSentEvents)]
        [InlineData(TransportType.LongPolling)]
        public async Task RequestToDisposedConnectionIdReturns404(TransportType transportType)
        {
            var manager = CreateConnectionManager();
            var connection = manager.CreateConnection();
            connection.Status = DefaultConnectionContext.ConnectionStatus.Disposed;

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context = MakeRequest("/foo", connection);
            SetTransport(context, transportType);

            var services = new ServiceCollection();
            services.AddEndPoint<TestEndPoint>();
            var builder = new SocketBuilder(services.BuildServiceProvider());
            builder.UseEndPoint<TestEndPoint>();
            var app = builder.Build();
            var options = new HttpSocketOptions();
            await dispatcher.ExecuteAsync(context, options, app);


            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        }

        [Fact]
        public async Task ConnectionStateSetToInactiveAfterPoll()
        {
            var manager = CreateConnectionManager();
            var connection = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context = MakeRequest("/foo", connection);

            var services = new ServiceCollection();
            services.AddEndPoint<TestEndPoint>();
            var builder = new SocketBuilder(services.BuildServiceProvider());
            builder.UseEndPoint<TestEndPoint>();
            var app = builder.Build();
            var options = new HttpSocketOptions();
            var task = dispatcher.ExecuteAsync(context, options, app);

            var buffer = Encoding.UTF8.GetBytes("Hello World");

            // Write to the transport so the poll yields
            await connection.Transport.Output.WriteAsync(buffer);

            await task;

            Assert.Equal(DefaultConnectionContext.ConnectionStatus.Inactive, connection.Status);
            Assert.Null(connection.GetHttpContext());

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public async Task BlockingConnectionWorksWithStreamingConnections()
        {
            var manager = CreateConnectionManager();
            var connection = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context = MakeRequest("/foo", connection);
            SetTransport(context, TransportType.ServerSentEvents);

            var services = new ServiceCollection();
            services.AddEndPoint<BlockingEndPoint>();
            var builder = new SocketBuilder(services.BuildServiceProvider());
            builder.UseEndPoint<BlockingEndPoint>();
            var app = builder.Build();
            var options = new HttpSocketOptions();
            var task = dispatcher.ExecuteAsync(context, options, app);

            var buffer = Encoding.UTF8.GetBytes("Hello World");

            // Write to the application
            await connection.Application.Output.WriteAsync(buffer);

            await task;

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            bool exists = manager.TryGetConnection(connection.ConnectionId, out _);
            Assert.False(exists);
        }

        [Fact]
        public async Task BlockingConnectionWorksWithLongPollingConnection()
        {
            var manager = CreateConnectionManager();
            var connection = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context = MakeRequest("/foo", connection);

            var services = new ServiceCollection();
            services.AddEndPoint<BlockingEndPoint>();
            var builder = new SocketBuilder(services.BuildServiceProvider());
            builder.UseEndPoint<BlockingEndPoint>();
            var app = builder.Build();
            var options = new HttpSocketOptions();
            var task = dispatcher.ExecuteAsync(context, options, app);

            var buffer = Encoding.UTF8.GetBytes("Hello World");

            // Write to the application
            await connection.Application.Output.WriteAsync(buffer);

            await task;

            Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
            bool exists = manager.TryGetConnection(connection.ConnectionId, out _);
            Assert.False(exists);
        }

        [Fact]
        public async Task AttemptingToPollWhileAlreadyPollingReplacesTheCurrentPoll()
        {
            var manager = CreateConnectionManager();
            var connection = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var services = new ServiceCollection();
            services.AddEndPoint<TestEndPoint>();
            var builder = new SocketBuilder(services.BuildServiceProvider());
            builder.UseEndPoint<TestEndPoint>();
            var app = builder.Build();
            var options = new HttpSocketOptions();

            var context1 = MakeRequest("/foo", connection);
            var task1 = dispatcher.ExecuteAsync(context1, options, app);
            var context2 = MakeRequest("/foo", connection);
            var task2 = dispatcher.ExecuteAsync(context2, options, app);

            // Task 1 should finish when request 2 arrives
            await task1.OrTimeout();

            // Send a message from the app to complete Task 2
            await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello, World"));

            await task2.OrTimeout();

            // Verify the results
            Assert.Equal(StatusCodes.Status204NoContent, context1.Response.StatusCode);
            Assert.Equal(string.Empty, GetContentAsString(context1.Response.Body));
            Assert.Equal(StatusCodes.Status200OK, context2.Response.StatusCode);
            Assert.Equal("Hello, World", GetContentAsString(context2.Response.Body));
        }

        [Fact]
        public async Task UnauthorizedConnectionFailsToStartEndPoint()
        {
            var manager = CreateConnectionManager();
            var connection = manager.CreateConnection();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddEndPoint<TestEndPoint>();
            services.AddAuthorizationPolicyEvaluator();
            services.AddAuthorization(o =>
            {
                o.AddPolicy("test", policy => policy.RequireClaim(ClaimTypes.NameIdentifier));
            });
            services.AddAuthenticationCore(o => o.AddScheme("Default", a => a.HandlerType = typeof(TestAuthenticationHandler)));
            services.AddLogging();
            var sp = services.BuildServiceProvider();
            context.Request.Path = "/foo";
            context.Request.Method = "GET";
            context.RequestServices = sp;
            var values = new Dictionary<string, StringValues>();
            values["id"] = connection.ConnectionId;
            var qs = new QueryCollection(values);
            context.Request.Query = qs;

            var builder = new SocketBuilder(sp);
            builder.UseEndPoint<TestEndPoint>();
            var app = builder.Build();
            var options = new HttpSocketOptions();
            options.AuthorizationPolicyNames.Add("test");

            // would hang if EndPoint was running
            await dispatcher.ExecuteAsync(context, options, app).OrTimeout();

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        }

        [Fact]
        public async Task AuthenticatedUserWithoutPermissionCausesForbidden()
        {
            var manager = CreateConnectionManager();
            var connection = manager.CreateConnection();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddEndPoint<TestEndPoint>();
            services.AddAuthorizationPolicyEvaluator();
            services.AddAuthorization(o =>
            {
                o.AddPolicy("test", policy => policy.RequireClaim(ClaimTypes.NameIdentifier));
            });
            services.AddAuthenticationCore(o => o.AddScheme("Default", a => a.HandlerType = typeof(TestAuthenticationHandler)));
            services.AddLogging();
            var sp = services.BuildServiceProvider();
            context.Request.Path = "/foo";
            context.Request.Method = "GET";
            context.RequestServices = sp;
            var values = new Dictionary<string, StringValues>();
            values["id"] = connection.ConnectionId;
            var qs = new QueryCollection(values);
            context.Request.Query = qs;

            var builder = new SocketBuilder(sp);
            builder.UseEndPoint<TestEndPoint>();
            var app = builder.Build();
            var options = new HttpSocketOptions();
            options.AuthorizationPolicyNames.Add("test");

            context.User = new ClaimsPrincipal(new ClaimsIdentity("authenticated"));

            // would hang if EndPoint was running
            await dispatcher.ExecuteAsync(context, options, app).OrTimeout();

            Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        }

        [Fact]
        public async Task AuthorizedConnectionCanConnectToEndPoint()
        {
            var manager = CreateConnectionManager();
            var connection = manager.CreateConnection();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddEndPoint<TestEndPoint>();
            services.AddAuthorizationPolicyEvaluator();
            services.AddAuthorization(o =>
            {
                o.AddPolicy("test", policy =>
                {
                    policy.RequireClaim(ClaimTypes.NameIdentifier);
                });
            });
            services.AddLogging();
            services.AddAuthenticationCore(o => o.AddScheme("Default", a => a.HandlerType = typeof(TestAuthenticationHandler)));
            var sp = services.BuildServiceProvider();
            context.Request.Path = "/foo";
            context.Request.Method = "GET";
            context.RequestServices = sp;
            var values = new Dictionary<string, StringValues>();
            values["id"] = connection.ConnectionId;
            var qs = new QueryCollection(values);
            context.Request.Query = qs;
            context.Response.Body = new MemoryStream();

            var builder = new SocketBuilder(sp);
            builder.UseEndPoint<TestEndPoint>();
            var app = builder.Build();
            var options = new HttpSocketOptions();
            options.AuthorizationPolicyNames.Add("test");

            // "authorize" user
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "name") }));

            var endPointTask = dispatcher.ExecuteAsync(context, options, app);
            await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello, World")).OrTimeout();

            await endPointTask.OrTimeout();

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Equal("Hello, World", GetContentAsString(context.Response.Body));
        }

 
        [Fact]
        public async Task AuthorizedConnectionWithAcceptedSchemesCanConnectToEndPoint()
        {
            var manager = CreateConnectionManager();
            var connection = manager.CreateConnection();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddEndPoint<TestEndPoint>();
            services.AddAuthorization(o =>
            {
                o.AddPolicy("test", policy =>
                {
                    policy.RequireClaim(ClaimTypes.NameIdentifier);
                    policy.AddAuthenticationSchemes("Default");
                });
            });
            services.AddAuthorizationPolicyEvaluator();
            services.AddLogging();
            services.AddAuthenticationCore(o => o.AddScheme("Default", a => a.HandlerType = typeof(TestAuthenticationHandler)));
            var sp = services.BuildServiceProvider();
            context.Request.Path = "/foo";
            context.Request.Method = "GET";
            context.RequestServices = sp;
            var values = new Dictionary<string, StringValues>();
            values["id"] = connection.ConnectionId;
            var qs = new QueryCollection(values);
            context.Request.Query = qs;
            context.Response.Body = new MemoryStream();

            var builder = new SocketBuilder(sp);
            builder.UseEndPoint<TestEndPoint>();
            var app = builder.Build();
            var options = new HttpSocketOptions();
            options.AuthorizationPolicyNames.Add("test");

            // "authorize" user
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "name") }));

            var endPointTask = dispatcher.ExecuteAsync(context, options, app);
            await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello, World")).OrTimeout();

            await endPointTask.OrTimeout();

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Equal("Hello, World", GetContentAsString(context.Response.Body));
        }

        [Fact]
        public async Task AuthorizedConnectionWithRejectedSchemesFailsToConnectToEndPoint()
        {
            var manager = CreateConnectionManager();
            var connection = manager.CreateConnection();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddEndPoint<TestEndPoint>();
            services.AddAuthorization(o =>
            {
                o.AddPolicy("test", policy =>
                {
                    policy.RequireClaim(ClaimTypes.NameIdentifier);
                    policy.AddAuthenticationSchemes("Default");
                });
            });
            services.AddAuthorizationPolicyEvaluator();
            services.AddLogging();
            services.AddAuthenticationCore(o => o.AddScheme("Default", a => a.HandlerType = typeof(RejectHandler)));
            var sp = services.BuildServiceProvider();
            context.Request.Path = "/foo";
            context.Request.Method = "GET";
            context.RequestServices = sp;
            var values = new Dictionary<string, StringValues>();
            values["id"] = connection.ConnectionId;
            var qs = new QueryCollection(values);
            context.Request.Query = qs;
            context.Response.Body = new MemoryStream();

            var builder = new SocketBuilder(sp);
            builder.UseEndPoint<TestEndPoint>();
            var app = builder.Build();
            var options = new HttpSocketOptions();
            options.AuthorizationPolicyNames.Add("test");

            // "authorize" user
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "name") }));

            // would block if EndPoint was executed
            await dispatcher.ExecuteAsync(context, options, app).OrTimeout();

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        }

        private class RejectHandler : TestAuthenticationHandler
        {
            protected override bool ShouldAccept => false;
        }

        private class TestAuthenticationHandler : IAuthenticationHandler
        {
            private HttpContext HttpContext;
            private AuthenticationScheme _scheme;

            protected virtual bool ShouldAccept { get => true; }

            public Task<AuthenticateResult> AuthenticateAsync()
            {
                if (ShouldAccept)
                {
                    return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(HttpContext.User, _scheme.Name)));
                }
                else
                {
                    return Task.FromResult(AuthenticateResult.None());
                }
            }

            public Task ChallengeAsync(AuthenticationProperties properties)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            public Task ForbidAsync(AuthenticationProperties properties)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
            {
                HttpContext = context;
                _scheme = scheme;
                return Task.CompletedTask;
            }

            public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task SignOutAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }
        }

        private static async Task CheckTransportSupported(TransportType supportedTransports, TransportType transportType, int status)
        {
            var manager = CreateConnectionManager();
            var connection = manager.CreateConnection();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());
            using (var strm = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                context.Response.Body = strm;
                var services = new ServiceCollection();
                services.AddOptions();
                services.AddEndPoint<ImmediatelyCompleteEndPoint>();
                SetTransport(context, transportType);
                context.Request.Path = "/foo";
                context.Request.Method = "GET";
                var values = new Dictionary<string, StringValues>();
                values["id"] = connection.ConnectionId;
                var qs = new QueryCollection(values);
                context.Request.Query = qs;

                var builder = new SocketBuilder(services.BuildServiceProvider());
                builder.UseEndPoint<ImmediatelyCompleteEndPoint>();
                var app = builder.Build();
                var options = new HttpSocketOptions();
                options.Transports = supportedTransports;

                await dispatcher.ExecuteAsync(context, options, app);
                Assert.Equal(status, context.Response.StatusCode);
                await strm.FlushAsync();

                // Check the message for 404
                if (status == 404)
                {
                    Assert.Equal($"{transportType} transport not supported by this end point type", Encoding.UTF8.GetString(strm.ToArray()));
                }
            }
        }

        private static DefaultHttpContext MakeRequest(string path, DefaultConnectionContext connection, string format = null)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Request.Method = "GET";
            var values = new Dictionary<string, StringValues>();
            values["id"] = connection.ConnectionId;
            if (format != null)
            {
                values["format"] = format;
            }
            var qs = new QueryCollection(values);
            context.Request.Query = qs;
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static void SetTransport(HttpContext context, TransportType transportType)
        {
            switch (transportType)
            {
                case TransportType.WebSockets:
                    context.Features.Set<IHttpWebSocketFeature>(new TestWebSocketConnectionFeature());
                    break;
                case TransportType.ServerSentEvents:
                    context.Request.Headers["Accept"] = "text/event-stream";
                    break;
                default:
                    break;
            }
        }

        private static ConnectionManager CreateConnectionManager()
        {
            return new ConnectionManager(new Logger<ConnectionManager>(new LoggerFactory()));
        }

        private string GetContentAsString(Stream body)
        {
            Assert.True(body.CanSeek, "Can't get content of a non-seekable stream");
            body.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(body))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public class NerverEndingEndPoint : EndPoint
    {
        public override Task OnConnectedAsync(ConnectionContext connection)
        {
            var tcs = new TaskCompletionSource<object>();
            return tcs.Task;
        }
    }

    public class BlockingEndPoint : EndPoint
    {
        public override Task OnConnectedAsync(ConnectionContext connection)
        {
            connection.Transport.Input.WaitToReadAsync().Wait();
            return Task.CompletedTask;
        }
    }

    public class SynchronusExceptionEndPoint : EndPoint
    {
        public override Task OnConnectedAsync(ConnectionContext connection)
        {
            throw new InvalidOperationException();
        }
    }

    public class ImmediatelyCompleteEndPoint : EndPoint
    {
        public override Task OnConnectedAsync(ConnectionContext connection)
        {
            return Task.CompletedTask;
        }
    }

    public class TestEndPoint : EndPoint
    {
        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            while (await connection.Transport.Input.WaitToReadAsync())
            {
            }
        }
    }
}

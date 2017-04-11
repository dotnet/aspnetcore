// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.SignalR.Tests.Common;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.AspNetCore.WebSockets.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.WebSockets.Internal;
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
                services.AddEndPoint<TestEndPoint>();
                services.AddOptions();
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
                services.AddOptions();
                services.AddEndPoint<TestEndPoint>();
                context.RequestServices = services.BuildServiceProvider();
                context.Request.Path = path;

                await dispatcher.ExecuteAsync<TestEndPoint>("", context);

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
        public async Task WebSocketTransportTimesOutWhenCloseFrameNotReceived()
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context = MakeRequest<ImmediatelyCompleteEndPoint>("/ws", state, isWebSocketRequest: true);

            var task = dispatcher.ExecuteAsync<ImmediatelyCompleteEndPoint>("", context);

            await task.OrTimeout();
        }

        [Theory]
        [InlineData("/ws", true)]
        [InlineData("/sse", false)]
        public async Task RequestToActiveConnectionId409ForStreamingTransports(string path, bool isWebSocketRequest)
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context1 = MakeRequest<TestEndPoint>(path, state, isWebSocketRequest: isWebSocketRequest);
            var context2 = MakeRequest<TestEndPoint>(path, state, isWebSocketRequest: isWebSocketRequest);

            var request1 = dispatcher.ExecuteAsync<TestEndPoint>("", context1);

            await dispatcher.ExecuteAsync<TestEndPoint>("", context2);

            Assert.Equal(StatusCodes.Status409Conflict, context2.Response.StatusCode);

            var webSocketTask = Task.CompletedTask;

            if (isWebSocketRequest)
            {
                var ws = (TestWebSocketConnectionFeature)context1.Features.Get<IHttpWebSocketConnectionFeature>();
                webSocketTask = ws.Client.ExecuteAsync(frame => Task.CompletedTask);
                await ws.Client.CloseAsync(new WebSocketCloseResult(WebSocketCloseStatus.NormalClosure), CancellationToken.None);
            }

            manager.CloseConnections();

            await webSocketTask.OrTimeout();

            await request1.OrTimeout();
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

        [Fact]
        public async Task AttemptingToPollWhileAlreadyPollingReplacesTheCurrentPoll()
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context1 = MakeRequest<BlockingEndPoint>("/poll", state);
            var task1 = dispatcher.ExecuteAsync<BlockingEndPoint>("", context1);
            var context2 = MakeRequest<BlockingEndPoint>("/poll", state);
            var task2 = dispatcher.ExecuteAsync<BlockingEndPoint>("", context2);

            // Task 1 should finish when request 2 arrives
            await task1.OrTimeout();

            // Send a message from the app to complete Task 2
            await state.Connection.Transport.Output.WriteAsync(new Message(Encoding.UTF8.GetBytes("Hello, World"), MessageType.Text));

            await task2.OrTimeout();

            // Verify the results
            Assert.Equal(StatusCodes.Status204NoContent, context1.Response.StatusCode);
            Assert.Equal(string.Empty, GetContentAsString(context1.Response.Body));
            Assert.Equal(StatusCodes.Status200OK, context2.Response.StatusCode);
            Assert.Equal("T12:T:Hello, World;", GetContentAsString(context2.Response.Body));
        }

        [Theory]
        [InlineData("", "text", "Hello, World", "Hello, World", MessageType.Text)] // Legacy format
        [InlineData("", "binary", "Hello, World", "Hello, World", MessageType.Binary)] // Legacy format
        [InlineData(TextContentType, null, "T12:T:Hello, World;", "Hello, World", MessageType.Text)]
        [InlineData(TextContentType, null, "T16:B:SGVsbG8sIFdvcmxk;", "Hello, World", MessageType.Binary)]
        [InlineData(TextContentType, null, "T12:E:Hello, World;", "Hello, World", MessageType.Error)]
        [InlineData(TextContentType, null, "T12:C:Hello, World;", "Hello, World", MessageType.Close)]
        [InlineData(BinaryContentType, null, "QgAAAAAAAAAMAEhlbGxvLCBXb3JsZA==", "Hello, World", MessageType.Text)]
        [InlineData(BinaryContentType, null, "QgAAAAAAAAAMAUhlbGxvLCBXb3JsZA==", "Hello, World", MessageType.Binary)]
        [InlineData(BinaryContentType, null, "QgAAAAAAAAAMAkhlbGxvLCBXb3JsZA==", "Hello, World", MessageType.Error)]
        [InlineData(BinaryContentType, null, "QgAAAAAAAAAMA0hlbGxvLCBXb3JsZA==", "Hello, World", MessageType.Close)]
        public async Task SendPutsPayloadsInTheChannel(string contentType, string format, string encoded, string payload, MessageType type)
        {
            var messages = await RunSendTest(contentType, encoded, format);

            Assert.Equal(1, messages.Count);
            Assert.Equal(payload, Encoding.UTF8.GetString(messages[0].Payload));
            Assert.Equal(type, messages[0].Type);
        }

        [Theory]
        [InlineData(TextContentType, "T12:T:Hello, World;16:B:SGVsbG8sIFdvcmxk;5:E:Error;6:C:Closed;")]
        [InlineData(BinaryContentType, "QgAAAAAAAAAMAEhlbGxvLCBXb3JsZAAAAAAAAAAMAUhlbGxvLCBXb3JsZAAAAAAAAAAFAkVycm9yAAAAAAAAAAYDQ2xvc2Vk")]
        public async Task SendAllowsMultipleMessages(string contentType, string encoded)
        {
            var messages = await RunSendTest(contentType, encoded, format: null);

            Assert.Equal(4, messages.Count);
            Assert.Equal("Hello, World", Encoding.UTF8.GetString(messages[0].Payload));
            Assert.Equal(MessageType.Text, messages[0].Type);
            Assert.Equal("Hello, World", Encoding.UTF8.GetString(messages[1].Payload));
            Assert.Equal(MessageType.Binary, messages[1].Type);
            Assert.Equal("Error", Encoding.UTF8.GetString(messages[2].Payload));
            Assert.Equal(MessageType.Error, messages[2].Type);
            Assert.Equal("Closed", Encoding.UTF8.GetString(messages[3].Payload));
            Assert.Equal(MessageType.Close, messages[3].Type);
        }

        [Fact]
        public async Task UnauthorizedConnectionFailsToStartEndPoint()
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddEndPoint<BlockingEndPoint>(options =>
            {
                options.AuthorizationPolicyNames.Add("test");
            });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("test", policy => policy.RequireClaim(ClaimTypes.NameIdentifier));
            });
            services.AddLogging();

            context.RequestServices = services.BuildServiceProvider();
            context.Request.Path = "/poll";
            var values = new Dictionary<string, StringValues>();
            values["id"] = state.Connection.ConnectionId;
            var qs = new QueryCollection(values);
            context.Request.Query = qs;
            var authFeature = new HttpAuthenticationFeature();
            authFeature.Handler = new TestAuthenticationHandler(context);
            context.Features.Set<IHttpAuthenticationFeature>(authFeature);

            // would hang if EndPoint was running
            await dispatcher.ExecuteAsync<BlockingEndPoint>("", context).OrTimeout();

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        }

        [Fact]
        public async Task AuthorizedConnectionCanConnectToEndPoint()
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddEndPoint<BlockingEndPoint>(options =>
            {
                options.AuthorizationPolicyNames.Add("test");
            });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("test", policy =>
                {
                    policy.RequireClaim(ClaimTypes.NameIdentifier);
                });
            });
            services.AddLogging();

            context.RequestServices = services.BuildServiceProvider();
            context.Request.Path = "/poll";
            var values = new Dictionary<string, StringValues>();
            values["id"] = state.Connection.ConnectionId;
            var qs = new QueryCollection(values);
            context.Request.Query = qs;
            context.Response.Body = new MemoryStream();
            var authFeature = new HttpAuthenticationFeature();
            authFeature.Handler = new TestAuthenticationHandler(context);
            context.Features.Set<IHttpAuthenticationFeature>(authFeature);

            // "authorize" user
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "name") }));

            var endPointTask = dispatcher.ExecuteAsync<BlockingEndPoint>("", context);
            await state.Connection.Transport.Output.WriteAsync(new Message(Encoding.UTF8.GetBytes("Hello, World"), MessageType.Text)).OrTimeout();

            await endPointTask.OrTimeout();

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Equal("T12:T:Hello, World;", GetContentAsString(context.Response.Body));
        }

        [Fact]
        public async Task AllPoliciesRequiredForAuthorizedEndPoint()
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddEndPoint<BlockingEndPoint>(options =>
            {
                options.AuthorizationPolicyNames.Add("test");
                options.AuthorizationPolicyNames.Add("secondPolicy");
            });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("test", policy => policy.RequireClaim(ClaimTypes.NameIdentifier));
                options.AddPolicy("secondPolicy", policy => policy.RequireClaim(ClaimTypes.StreetAddress));
            });
            services.AddLogging();

            context.RequestServices = services.BuildServiceProvider();
            context.Request.Path = "/poll";
            var values = new Dictionary<string, StringValues>();
            values["id"] = state.Connection.ConnectionId;
            var qs = new QueryCollection(values);
            context.Request.Query = qs;
            context.Response.Body = new MemoryStream();
            var authFeature = new HttpAuthenticationFeature();
            authFeature.Handler = new TestAuthenticationHandler(context);
            context.Features.Set<IHttpAuthenticationFeature>(authFeature);

            // partialy "authorize" user
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "name") }));

            // would hang if EndPoint was running
            await dispatcher.ExecuteAsync<BlockingEndPoint>("", context).OrTimeout();

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);

            // fully "authorize" user
            context.User.AddIdentity(new ClaimsIdentity(new[] { new Claim(ClaimTypes.StreetAddress, "12345 123rd St. NW") }));

            var endPointTask = dispatcher.ExecuteAsync<BlockingEndPoint>("", context);
            await state.Connection.Transport.Output.WriteAsync(new Message(Encoding.UTF8.GetBytes("Hello, World"), MessageType.Text)).OrTimeout();

            await endPointTask.OrTimeout();

            Assert.Equal("T12:T:Hello, World;", GetContentAsString(context.Response.Body));
        }

        [Fact]
        public async Task AuthorizedConnectionWithAcceptedSchemesCanConnectToEndPoint()
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddEndPoint<BlockingEndPoint>(options =>
            {
                options.AuthorizationPolicyNames.Add("test");
            });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("test", policy =>
                {
                    policy.RequireClaim(ClaimTypes.NameIdentifier);
                    policy.AddAuthenticationSchemes("Default");
                });
            });
            services.AddLogging();

            context.RequestServices = services.BuildServiceProvider();
            context.Request.Path = "/poll";
            var values = new Dictionary<string, StringValues>();
            values["id"] = state.Connection.ConnectionId;
            var qs = new QueryCollection(values);
            context.Request.Query = qs;
            context.Response.Body = new MemoryStream();
            var authFeature = new HttpAuthenticationFeature();
            authFeature.Handler = new TestAuthenticationHandler(context);
            context.Features.Set<IHttpAuthenticationFeature>(authFeature);

            // "authorize" user
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "name") }));

            var endPointTask = dispatcher.ExecuteAsync<BlockingEndPoint>("", context);
            await state.Connection.Transport.Output.WriteAsync(new Message(Encoding.UTF8.GetBytes("Hello, World"), MessageType.Text)).OrTimeout();

            await endPointTask.OrTimeout();

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Equal("T12:T:Hello, World;", GetContentAsString(context.Response.Body));
        }

        [Fact]
        public async Task AuthorizedConnectionWithRejectedSchemesFailsToConnectToEndPoint()
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddEndPoint<BlockingEndPoint>(options =>
            {
                options.AuthorizationPolicyNames.Add("test");
            });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("test", policy =>
                {
                    policy.RequireClaim(ClaimTypes.NameIdentifier);
                    policy.AddAuthenticationSchemes("Default");
                });
            });
            services.AddLogging();

            context.RequestServices = services.BuildServiceProvider();
            context.Request.Path = "/poll";
            var values = new Dictionary<string, StringValues>();
            values["id"] = state.Connection.ConnectionId;
            var qs = new QueryCollection(values);
            context.Request.Query = qs;
            context.Response.Body = new MemoryStream();
            var authFeature = new HttpAuthenticationFeature();
            authFeature.Handler = new TestAuthenticationHandler(context, acceptScheme: false);
            context.Features.Set<IHttpAuthenticationFeature>(authFeature);

            // "authorize" user
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "name") }));

            // would block if EndPoint was executed
            await dispatcher.ExecuteAsync<BlockingEndPoint>("", context).OrTimeout();

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        }

        private class TestAuthenticationHandler : IAuthenticationHandler
        {
            private readonly HttpContext HttpContext;
            private readonly bool _acceptScheme;

            public TestAuthenticationHandler(HttpContext context, bool acceptScheme = true)
            {
                HttpContext = context;
                _acceptScheme = acceptScheme;
            }

            public Task AuthenticateAsync(AuthenticateContext context)
            {
                if (_acceptScheme)
                {
                    context.Authenticated(HttpContext.User, context.Properties, context.Description);
                }
                else
                {
                    context.NotAuthenticated();
                }
                return Task.CompletedTask;
            }

            public Task ChallengeAsync(ChallengeContext context)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Accept();
                return Task.CompletedTask;
            }

            public void GetDescriptions(DescribeSchemesContext context)
            {
                throw new NotImplementedException();
            }

            public Task SignInAsync(SignInContext context)
            {
                throw new NotImplementedException();
            }

            public Task SignOutAsync(SignOutContext context)
            {
                throw new NotImplementedException();
            }
        }

        private static async Task CheckTransportSupported(TransportType supportedTransports, TransportType transportType, int status)
        {
            var path = "";
            switch (transportType)
            {
                case TransportType.WebSockets:
                    path = "/ws";
                    break;
                case TransportType.ServerSentEvents:
                    path = "/sse";
                    break;
                case TransportType.LongPolling:
                    path = "/poll";
                    break;
                default:
                    break;
            }

            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();
            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());
            using (var strm = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                context.Response.Body = strm;
                var services = new ServiceCollection();
                services.AddOptions();
                services.AddEndPoint<ImmediatelyCompleteEndPoint>(options =>
                {
                    options.Transports = supportedTransports;
                });

                context.RequestServices = services.BuildServiceProvider();
                context.Request.Path = path;
                var values = new Dictionary<string, StringValues>();
                values["id"] = state.Connection.ConnectionId;
                var qs = new QueryCollection(values);
                context.Request.Query = qs;
                await dispatcher.ExecuteAsync<ImmediatelyCompleteEndPoint>("", context);
                Assert.Equal(status, context.Response.StatusCode);
                await strm.FlushAsync();

                // Check the message for 404
                if (status == 404)
                {
                    Assert.Equal($"{transportType} transport not supported by this end point type", Encoding.UTF8.GetString(strm.ToArray()));
                }
            }
        }

        private static async Task<List<Message>> RunSendTest(string contentType, string encoded, string format)
        {
            var manager = CreateConnectionManager();
            var state = manager.CreateConnection();

            var dispatcher = new HttpConnectionDispatcher(manager, new LoggerFactory());

            var context = MakeRequest<TestEndPoint>("/send", state, format);
            context.Request.ContentType = contentType;
            var endPoint = context.RequestServices.GetRequiredService<TestEndPoint>();

            var buffer = contentType == BinaryContentType ?
                Convert.FromBase64String(encoded) :
                Encoding.UTF8.GetBytes(encoded);
            var messages = new List<Message>();
            using (context.Request.Body = new MemoryStream(buffer, writable: false))
            {
                await dispatcher.ExecuteAsync<TestEndPoint>("", context).OrTimeout();
            }

            while (state.Connection.Transport.Input.TryRead(out var message))
            {
                messages.Add(message);
            }

            return messages;
        }

        private static DefaultHttpContext MakeRequest<TEndPoint>(string path, ConnectionState state, string format = null, bool isWebSocketRequest = false) where TEndPoint : EndPoint
        {
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddEndPoint<TEndPoint>(o =>
            {
                // Make the close timeout less than the default for OrTimeout() test helper
                o.WebSockets.CloseTimeout = TimeSpan.FromSeconds(1);
            });

            services.AddOptions();
            context.RequestServices = services.BuildServiceProvider();
            context.Request.Path = path;
            var values = new Dictionary<string, StringValues>();
            values["id"] = state.Connection.ConnectionId;
            if (format != null)
            {
                values["format"] = format;
            }
            var qs = new QueryCollection(values);
            context.Request.Query = qs;
            context.Response.Body = new MemoryStream();

            if (isWebSocketRequest)
            {
                // Add Test WebSocket feature
                context.Features.Set<IHttpWebSocketConnectionFeature>(new TestWebSocketConnectionFeature());
            }

            return context;
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
        public override Task OnConnectedAsync(Connection connection)
        {
            var tcs = new TaskCompletionSource<object>();
            return tcs.Task;
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

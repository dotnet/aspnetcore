// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IdentityModel.Tokens.Jwt;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Abstractions;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Http.Connections.Tests;

public class HttpConnectionDispatcherTests : VerifiableLoggedTest
{
    [Fact]
    public async Task NegotiateVersionZeroReservesConnectionIdAndReturnsIt()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            services.AddOptions();
            var ms = new MemoryStream();
            context.Request.Path = "/foo";
            context.Request.Method = "POST";
            context.Response.Body = ms;
            await dispatcher.ExecuteNegotiateAsync(context, new HttpConnectionDispatcherOptions());
            var negotiateResponse = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(ms.ToArray()));
            var connectionId = negotiateResponse.Value<string>("connectionId");
            var connectionToken = negotiateResponse.Value<string>("connectionToken");
            Assert.Null(connectionToken);
            Assert.NotNull(connectionId);
        }
    }

    [Fact]
    public async Task NegotiateReservesConnectionTokenAndConnectionIdAndReturnsIt()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            services.AddOptions();
            var ms = new MemoryStream();
            context.Request.Path = "/foo";
            context.Request.Method = "POST";
            context.Response.Body = ms;
            context.Request.QueryString = new QueryString("?negotiateVersion=1");
            await dispatcher.ExecuteNegotiateAsync(context, new HttpConnectionDispatcherOptions());
            var negotiateResponse = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(ms.ToArray()));
            var connectionId = negotiateResponse.Value<string>("connectionId");
            var connectionToken = negotiateResponse.Value<string>("connectionToken");
            Assert.True(manager.TryGetConnection(connectionToken, out var connectionContext));
            Assert.Equal(connectionId, connectionContext.ConnectionId);
            Assert.NotEqual(connectionId, connectionToken);
        }
    }

    [Fact]
    public async Task CheckThatThresholdValuesAreEnforced()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            services.AddOptions();
            var ms = new MemoryStream();
            context.Request.Path = "/foo";
            context.Request.Method = "POST";
            context.Response.Body = ms;
            context.Request.QueryString = new QueryString("?negotiateVersion=1");
            var options = new HttpConnectionDispatcherOptions { TransportMaxBufferSize = 4, ApplicationMaxBufferSize = 4 };
            await dispatcher.ExecuteNegotiateAsync(context, options);
            var negotiateResponse = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(ms.ToArray()));
            var connectionToken = negotiateResponse.Value<string>("connectionToken");
            context.Request.QueryString = context.Request.QueryString.Add("id", connectionToken);
            Assert.True(manager.TryGetConnection(connectionToken, out var connection));
            // Fake actual connection after negotiate to populate the pipes on the connection
            await dispatcher.ExecuteAsync(context, options, c => Task.CompletedTask);

            // This write should complete immediately but it exceeds the writer threshold
            var writeTask = connection.Application.Output.WriteAsync(new[] { (byte)'b', (byte)'y', (byte)'t', (byte)'e', (byte)'s' });

            Assert.False(writeTask.IsCompleted);

            // Reading here puts us below the threshold
            await connection.Transport.Input.ConsumeAsync(5);

            await writeTask.AsTask().DefaultTimeout();
        }
    }

    [Fact]
    public async Task InvalidNegotiateProtocolVersionThrows()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            services.AddOptions();
            var ms = new MemoryStream();
            context.Request.Path = "/foo";
            context.Request.Method = "POST";
            context.Response.Body = ms;
            context.Request.QueryString = new QueryString("?negotiateVersion=Invalid");
            var options = new HttpConnectionDispatcherOptions { TransportMaxBufferSize = 4, ApplicationMaxBufferSize = 4 };
            await dispatcher.ExecuteNegotiateAsync(context, options);
            var negotiateResponse = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(ms.ToArray()));

            var error = negotiateResponse.Value<string>("error");
            Assert.Equal("The client requested an invalid protocol version 'Invalid'", error);

            var connectionId = negotiateResponse.Value<string>("connectionId");
            Assert.Null(connectionId);
        }
    }

    [Fact]
    public async Task NoNegotiateVersionInQueryStringThrowsWhenMinProtocolVersionIsSet()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            services.AddOptions();
            var ms = new MemoryStream();
            context.Request.Path = "/foo";
            context.Request.Method = "POST";
            context.Response.Body = ms;
            context.Request.QueryString = new QueryString("");
            var options = new HttpConnectionDispatcherOptions { TransportMaxBufferSize = 4, ApplicationMaxBufferSize = 4, MinimumProtocolVersion = 1 };
            await dispatcher.ExecuteNegotiateAsync(context, options);
            var negotiateResponse = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(ms.ToArray()));

            var error = negotiateResponse.Value<string>("error");
            Assert.Equal("The client requested version '0', but the server does not support this version.", error);

            var connectionId = negotiateResponse.Value<string>("connectionId");
            Assert.Null(connectionId);
        }
    }

    [Theory]
    [InlineData(HttpTransportType.LongPolling)]
    [InlineData(HttpTransportType.ServerSentEvents)]
    public async Task CheckThatThresholdValuesAreEnforcedWithSends(HttpTransportType transportType)
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var options = new HttpConnectionDispatcherOptions();
            options.TransportMaxBufferSize = 8;
            options.ApplicationMaxBufferSize = 8;
            var connection = manager.CreateConnection(options);
            connection.TransportType = transportType;

            using (var requestBody = new MemoryStream())
            using (var responseBody = new MemoryStream())
            {
                var bytes = Encoding.UTF8.GetBytes("EXTRADATA Hi");
                requestBody.Write(bytes, 0, bytes.Length);
                requestBody.Seek(0, SeekOrigin.Begin);

                var context = new DefaultHttpContext();
                context.Request.Body = requestBody;
                context.Response.Body = responseBody;

                var services = new ServiceCollection();
                services.AddSingleton<TestConnectionHandler>();
                services.AddOptions();
                context.Request.Path = "/foo";
                context.Request.Method = "POST";
                var values = new Dictionary<string, StringValues>();
                values["id"] = connection.ConnectionToken;
                values["negotiateVersion"] = "1";
                var qs = new QueryCollection(values);
                context.Request.Query = qs;

                var builder = new ConnectionBuilder(services.BuildServiceProvider());
                builder.UseConnectionHandler<TestConnectionHandler>();
                var app = builder.Build();

                // This task should complete immediately but it exceeds the writer threshold
                var executeTask = dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);
                Assert.False(executeTask.IsCompleted);
                await connection.Transport.Input.ConsumeAsync(10);
                await executeTask.DefaultTimeout();

                Assert.True(connection.Transport.Input.TryRead(out var result));
                Assert.Equal("Hi", Encoding.UTF8.GetString(result.Buffer.ToArray()));
                connection.Transport.Input.AdvanceTo(result.Buffer.End);
            }
        }
    }

    [Theory]
    [InlineData(HttpTransportType.LongPolling | HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents)]
    [InlineData(HttpTransportType.None)]
    [InlineData(HttpTransportType.LongPolling | HttpTransportType.WebSockets)]
    public async Task NegotiateReturnsAvailableTransportsAfterFilteringByOptions(HttpTransportType transports)
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = new DefaultHttpContext();
            context.Features.Set<IHttpResponseFeature>(new ResponseFeature());
            context.Features.Set<IHttpWebSocketFeature>(new TestWebSocketConnectionFeature());
            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            services.AddOptions();
            var ms = new MemoryStream();
            context.Request.Path = "/foo";
            context.Request.Method = "POST";
            context.Response.Body = ms;
            context.Request.QueryString = new QueryString("?negotiateVersion=1");
            await dispatcher.ExecuteNegotiateAsync(context, new HttpConnectionDispatcherOptions { Transports = transports });

            var negotiateResponse = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(ms.ToArray()));
            var availableTransports = HttpTransportType.None;
            foreach (var transport in negotiateResponse["availableTransports"])
            {
                var transportType = (HttpTransportType)Enum.Parse(typeof(HttpTransportType), transport.Value<string>("transport"));
                availableTransports |= transportType;
            }

            Assert.Equal(transports, availableTransports);
        }
    }

    [Theory]
    [InlineData(HttpTransportType.WebSockets)]
    [InlineData(HttpTransportType.ServerSentEvents)]
    [InlineData(HttpTransportType.LongPolling)]
    public async Task EndpointsThatAcceptConnectionId404WhenUnknownConnectionIdProvided(HttpTransportType transportType)
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            using (var strm = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                context.Features.Set<IHttpResponseFeature>(new ResponseFeature());
                context.Response.Body = strm;

                var services = new ServiceCollection();
                services.AddSingleton<TestConnectionHandler>();
                services.AddOptions();
                context.Request.Path = "/foo";
                context.Request.Method = "GET";
                var values = new Dictionary<string, StringValues>();
                values["id"] = "unknown";
                values["negotiateVersion"] = "1";
                var qs = new QueryCollection(values);
                context.Request.Query = qs;
                SetTransport(context, transportType);

                var builder = new ConnectionBuilder(services.BuildServiceProvider());
                builder.UseConnectionHandler<TestConnectionHandler>();
                var app = builder.Build();
                await dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);

                Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
                await strm.FlushAsync();
                Assert.Equal("No Connection with that ID", Encoding.UTF8.GetString(strm.ToArray()));

                if (transportType == HttpTransportType.LongPolling)
                {
                    AssertResponseHasCacheHeaders(context.Response);
                }
            }
        }
    }

    [Fact]
    public async Task EndpointsThatAcceptConnectionId404WhenUnknownConnectionIdProvidedForPost()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            using (var strm = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                context.Response.Body = strm;

                var services = new ServiceCollection();
                services.AddSingleton<TestConnectionHandler>();
                services.AddOptions();
                context.Request.Path = "/foo";
                context.Request.Method = "POST";
                var values = new Dictionary<string, StringValues>();
                values["id"] = "unknown";
                values["negotiateVersion"] = "1";
                var qs = new QueryCollection(values);
                context.Request.Query = qs;

                var builder = new ConnectionBuilder(services.BuildServiceProvider());
                builder.UseConnectionHandler<TestConnectionHandler>();
                var app = builder.Build();
                await dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);

                Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
                await strm.FlushAsync();
                Assert.Equal("No Connection with that ID", Encoding.UTF8.GetString(strm.ToArray()));
            }
        }
    }

    [Fact]
    public async Task PostNotAllowedForWebSocketConnections()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.WebSockets;

            using (var strm = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                context.Response.Body = strm;

                var services = new ServiceCollection();
                services.AddSingleton<TestConnectionHandler>();
                services.AddOptions();
                context.Request.Path = "/foo";
                context.Request.Method = "POST";
                var values = new Dictionary<string, StringValues>();
                values["id"] = connection.ConnectionToken;
                values["negotiateVersion"] = "1";
                var qs = new QueryCollection(values);
                context.Request.Query = qs;

                var builder = new ConnectionBuilder(services.BuildServiceProvider());
                builder.UseConnectionHandler<TestConnectionHandler>();
                var app = builder.Build();
                await dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);

                Assert.Equal(StatusCodes.Status405MethodNotAllowed, context.Response.StatusCode);
                await strm.FlushAsync();
                Assert.Equal("POST requests are not allowed for WebSocket connections.", Encoding.UTF8.GetString(strm.ToArray()));
            }
        }
    }

    [Fact]
    public async Task PostReturns404IfConnectionDisposed()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;
            await connection.DisposeAsync(closeGracefully: false);

            using (var strm = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                context.Response.Body = strm;

                var services = new ServiceCollection();
                services.AddSingleton<TestConnectionHandler>();
                services.AddOptions();
                context.Request.Path = "/foo";
                context.Request.Method = "POST";
                var values = new Dictionary<string, StringValues>();
                values["id"] = connection.ConnectionId;
                values["negotiateVersion"] = "1";
                var qs = new QueryCollection(values);
                context.Request.Query = qs;

                var builder = new ConnectionBuilder(services.BuildServiceProvider());
                builder.UseConnectionHandler<TestConnectionHandler>();
                var app = builder.Build();
                await dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);

                Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
            }
        }
    }

    [Theory]
    [InlineData(HttpTransportType.ServerSentEvents)]
    [InlineData(HttpTransportType.WebSockets)]
    public async Task TransportEndingGracefullyWaitsOnApplication(HttpTransportType transportType)
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var connection = manager.CreateConnection();

            using (var strm = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                SetTransport(context, transportType);
                var cts = new CancellationTokenSource();
                context.Response.Body = strm;
                context.RequestAborted = cts.Token;

                var services = new ServiceCollection();
                services.AddSingleton<TestConnectionHandler>();
                services.AddOptions();
                context.Request.Path = "/foo";
                context.Request.Method = "GET";
                var values = new Dictionary<string, StringValues>();
                values["id"] = connection.ConnectionToken;
                values["negotiateVersion"] = "1";
                var qs = new QueryCollection(values);
                context.Request.Query = qs;

                var builder = new ConnectionBuilder(services.BuildServiceProvider());
                builder.Use(next =>
                {
                    return async connectionContext =>
                    {
                        // Ensure both sides of the pipe are ok
                        var result = await connectionContext.Transport.Input.ReadAsync();
                        Assert.True(result.IsCompleted);
                        await connectionContext.Transport.Output.WriteAsync(result.Buffer.First);
                    };
                });

                var app = builder.Build();
                var task = dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);

                // Pretend the transport closed because the client disconnected
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var ws = (TestWebSocketConnectionFeature)context.Features.Get<IHttpWebSocketFeature>();
                    await ws.Client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", default);
                }
                else
                {
                    cts.Cancel();
                }

                await task.DefaultTimeout();

                await connection.ApplicationTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task TransportEndingGracefullyWaitsOnApplicationLongPolling()
    {
        using (StartVerifiableLog())
        {
            var disconnectTimeout = TimeSpan.FromSeconds(5);
            var manager = CreateConnectionManager(LoggerFactory, disconnectTimeout);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;

            using (var strm = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                SetTransport(context, HttpTransportType.LongPolling);
                var cts = new CancellationTokenSource();
                context.Response.Body = strm;
                context.RequestAborted = cts.Token;

                var services = new ServiceCollection();
                services.AddSingleton<TestConnectionHandler>();
                services.AddOptions();
                context.Request.Path = "/foo";
                context.Request.Method = "GET";
                var values = new Dictionary<string, StringValues>();
                values["id"] = connection.ConnectionToken;
                values["negotiateVersion"] = "1";
                var qs = new QueryCollection(values);
                context.Request.Query = qs;
                context.RequestServices = services.BuildServiceProvider();

                var builder = new ConnectionBuilder(services.BuildServiceProvider());
                builder.Use(next =>
                {
                    return async connectionContext =>
                    {
                        // Ensure both sides of the pipe are ok
                        var result = await connectionContext.Transport.Input.ReadAsync();
                        Assert.True(result.IsCompleted);
                        await connectionContext.Transport.Output.WriteAsync(result.Buffer.First);
                    };
                });

                var app = builder.Build();
                var task = dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);

                // Pretend the transport closed because the client disconnected
                cts.Cancel();

                await task.DefaultTimeout();

                // We've been gone longer than the expiration time
                connection.LastSeenTicks = TimeSpan.FromMilliseconds(Environment.TickCount64) - disconnectTimeout - TimeSpan.FromTicks(1);

                // The application is still running here because the poll is only killed
                // by the heartbeat so we pretend to do a scan and this should force the application task to complete
                manager.Scan();

                // The application task should complete gracefully
                await connection.ApplicationTask.DefaultTimeout();
            }
        }
    }

    [Theory]
    [InlineData(HttpTransportType.LongPolling)]
    [InlineData(HttpTransportType.ServerSentEvents)]
    public async Task PostSendsToConnection(HttpTransportType transportType)
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = transportType;

            using (var requestBody = new MemoryStream())
            using (var responseBody = new MemoryStream())
            {
                var bytes = Encoding.UTF8.GetBytes("Hello World");
                requestBody.Write(bytes, 0, bytes.Length);
                requestBody.Seek(0, SeekOrigin.Begin);

                var context = new DefaultHttpContext();
                context.Request.Body = requestBody;
                context.Response.Body = responseBody;

                var services = new ServiceCollection();
                services.AddSingleton<TestConnectionHandler>();
                services.AddOptions();
                context.Request.Path = "/foo";
                context.Request.Method = "POST";
                var values = new Dictionary<string, StringValues>();
                values["id"] = connection.ConnectionToken;
                values["negotiateVersion"] = "1";
                var qs = new QueryCollection(values);
                context.Request.Query = qs;

                var builder = new ConnectionBuilder(services.BuildServiceProvider());
                builder.UseConnectionHandler<TestConnectionHandler>();
                var app = builder.Build();

                Assert.Equal(0, connection.ApplicationStream.Length);

                await dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);

                Assert.True(connection.Transport.Input.TryRead(out var result));
                Assert.Equal("Hello World", Encoding.UTF8.GetString(result.Buffer.ToArray()));
                Assert.Equal(0, connection.ApplicationStream.Length);
                connection.Transport.Input.AdvanceTo(result.Buffer.End);
            }
        }
    }

    [Theory]
    [InlineData(HttpTransportType.LongPolling)]
    [InlineData(HttpTransportType.ServerSentEvents)]
    public async Task PostSendsToConnectionInParallel(HttpTransportType transportType)
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = transportType;

            // Allow a maximum of one caller to use code at one time
            var callerTracker = new SemaphoreSlim(1, 1);
            var waitTcs = new TaskCompletionSource();

            // This tests thread safety of sending multiple pieces of data to a connection at once
            var executeTask1 = DispatcherExecuteAsync(dispatcher, connection, callerTracker, waitTcs.Task);
            var executeTask2 = DispatcherExecuteAsync(dispatcher, connection, callerTracker, waitTcs.Task);

            waitTcs.SetResult();

            await Task.WhenAll(executeTask1, executeTask2);
        }

        async Task DispatcherExecuteAsync(HttpConnectionDispatcher dispatcher, HttpConnectionContext connection, SemaphoreSlim callerTracker, Task waitTask)
        {
            using (var requestBody = new TrackingMemoryStream(callerTracker, waitTask))
            {
                var bytes = Encoding.UTF8.GetBytes("Hello World");
                requestBody.Write(bytes, 0, bytes.Length);
                requestBody.Seek(0, SeekOrigin.Begin);

                var context = new DefaultHttpContext();
                context.Request.Body = requestBody;

                var services = new ServiceCollection();
                services.AddSingleton<TestConnectionHandler>();
                services.AddOptions();
                context.Request.Path = "/foo";
                context.Request.Method = "POST";
                var values = new Dictionary<string, StringValues>();
                values["id"] = connection.ConnectionId;
                values["negotiateVersion"] = "1";
                var qs = new QueryCollection(values);
                context.Request.Query = qs;

                var builder = new ConnectionBuilder(services.BuildServiceProvider());
                builder.UseConnectionHandler<TestConnectionHandler>();
                var app = builder.Build();

                await dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);
            }
        }
    }

    private class TrackingMemoryStream : MemoryStream
    {
        private readonly SemaphoreSlim _callerTracker;
        private readonly Task _waitTask;

        public TrackingMemoryStream(SemaphoreSlim callerTracker, Task waitTask)
        {
            _callerTracker = callerTracker;
            _waitTask = waitTask;
        }

        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            // Will return false if all available locks from semaphore are taken
            if (!_callerTracker.Wait(0))
            {
                throw new Exception("Too many callers.");
            }

            try
            {
                await _waitTask;

                await base.CopyToAsync(destination, bufferSize, cancellationToken);
            }
            finally
            {
                _callerTracker.Release();
            }
        }
    }

    [Fact]
    public async Task ResponsesForLongPollingHaveCacheHeaders()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();

            var context = MakeRequest("/foo", connection, services);

            // Initial poll will complete immediately
            await dispatcher.ExecuteAsync(context, options, app).DefaultTimeout();
            AssertResponseHasCacheHeaders(context.Response);

            var pollContext = MakeRequest("/foo", connection, services);
            var pollTask = dispatcher.ExecuteAsync(pollContext, options, app);
            connection.Transport.Output.Complete();
            await pollTask.DefaultTimeout();

            AssertResponseHasCacheHeaders(pollContext.Response);
        }
    }

    [Fact]
    public async Task HttpContextFeatureForLongpollingWorksBetweenPolls()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;

            using (var requestBody = new MemoryStream())
            using (var responseBody = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                context.Request.Body = requestBody;
                context.Response.Body = responseBody;

                var services = new ServiceCollection();
                services.AddSingleton<HttpContextConnectionHandler>();
                services.AddOptions();

                // Setup state on the HttpContext
                context.Request.Path = "/foo";
                context.Request.Method = "GET";
                var values = new Dictionary<string, StringValues>();
                values["id"] = connection.ConnectionToken;
                values["negotiateVersion"] = "1";
                values["another"] = "value";
                var qs = new QueryCollection(values);
                context.Request.Query = qs;
                context.Request.Headers["header1"] = "h1";
                context.Request.Headers["header2"] = "h2";
                context.Request.Headers["header3"] = "h3";
                context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("claim1", "claimValue") }));
                context.TraceIdentifier = "requestid";
                context.Connection.Id = "connectionid";
                context.Connection.LocalIpAddress = IPAddress.Loopback;
                context.Connection.LocalPort = 4563;
                context.Connection.RemoteIpAddress = IPAddress.IPv6Any;
                context.Connection.RemotePort = 43456;
                context.SetEndpoint(new Endpoint(null, null, "TestName"));
                context.RequestServices = services.BuildServiceProvider();

                var builder = new ConnectionBuilder(services.BuildServiceProvider());
                builder.UseConnectionHandler<HttpContextConnectionHandler>();
                var app = builder.Build();

                // Start a poll
                var task = dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);
                Assert.True(task.IsCompleted);
                Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

                task = dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);

                // Send to the application
                var buffer = Encoding.UTF8.GetBytes("Hello World");
                await connection.Application.Output.WriteAsync(buffer);

                // The poll request should end
                await task;

                // Make sure the actual response isn't affected
                Assert.Equal("application/octet-stream", context.Response.ContentType);

                // Now do a new send again without the poll (that request should have ended)
                await connection.Application.Output.WriteAsync(buffer);

                connection.Application.Output.Complete();

                // Wait for the endpoint to end
                await connection.ApplicationTask;

                var connectionHttpContext = connection.GetHttpContext();
                Assert.NotNull(connectionHttpContext);

                Assert.Equal(3, connectionHttpContext.Request.Query.Count);
                Assert.Equal("value", connectionHttpContext.Request.Query["another"]);

                Assert.Equal(3, connectionHttpContext.Request.Headers.Count);
                Assert.Equal("h1", connectionHttpContext.Request.Headers["header1"]);
                Assert.Equal("h2", connectionHttpContext.Request.Headers["header2"]);
                Assert.Equal("h3", connectionHttpContext.Request.Headers["header3"]);
                Assert.Equal("requestid", connectionHttpContext.TraceIdentifier);
                Assert.Equal("claimValue", connectionHttpContext.User.Claims.FirstOrDefault().Value);
                Assert.Equal("connectionid", connectionHttpContext.Connection.Id);
                Assert.Equal(IPAddress.Loopback, connectionHttpContext.Connection.LocalIpAddress);
                Assert.Equal(4563, connectionHttpContext.Connection.LocalPort);
                Assert.Equal(IPAddress.IPv6Any, connectionHttpContext.Connection.RemoteIpAddress);
                Assert.Equal(43456, connectionHttpContext.Connection.RemotePort);
                Assert.NotNull(connectionHttpContext.RequestServices);
                Assert.Equal(Stream.Null, connectionHttpContext.Response.Body);
                Assert.NotNull(connectionHttpContext.Response.Headers);
                Assert.Equal("application/xml", connectionHttpContext.Response.ContentType);
                var endpointFeature = connectionHttpContext.Features.Get<IEndpointFeature>();
                Assert.NotNull(endpointFeature);
                Assert.Equal("TestName", endpointFeature.Endpoint.DisplayName);
            }
        }
    }

    [Theory]
    [InlineData(HttpTransportType.ServerSentEvents)]
    [InlineData(HttpTransportType.LongPolling)]
    public async Task EndpointsThatRequireConnectionId400WhenNoConnectionIdProvided(HttpTransportType transportType)
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            using (var strm = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                context.Features.Set<IHttpResponseFeature>(new ResponseFeature());
                context.Response.Body = strm;
                var services = new ServiceCollection();
                services.AddOptions();
                services.AddSingleton<TestConnectionHandler>();
                context.Request.Path = "/foo";
                context.Request.Method = "GET";
                context.Request.QueryString = new QueryString("?negotiateVersion=1");

                SetTransport(context, transportType);

                var builder = new ConnectionBuilder(services.BuildServiceProvider());
                builder.UseConnectionHandler<TestConnectionHandler>();
                var app = builder.Build();
                await dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);

                Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
                await strm.FlushAsync();
                Assert.Equal("Connection ID required", Encoding.UTF8.GetString(strm.ToArray()));

                if (transportType == HttpTransportType.LongPolling)
                {
                    AssertResponseHasCacheHeaders(context.Response);
                }
            }
        }
    }

    [Theory]
    [InlineData(HttpTransportType.LongPolling)]
    [InlineData(HttpTransportType.ServerSentEvents)]
    public async Task IOExceptionWhenReadingRequestReturns400Response(HttpTransportType transportType)
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = transportType;

            var mockStream = new Mock<Stream>();
            mockStream.Setup(m => m.CopyToAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Throws(new IOException());

            using (var responseBody = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                context.Request.Body = mockStream.Object;
                context.Response.Body = responseBody;

                var services = new ServiceCollection();
                services.AddSingleton<TestConnectionHandler>();
                services.AddOptions();
                context.Request.Path = "/foo";
                context.Request.Method = "POST";
                var values = new Dictionary<string, StringValues>();
                values["id"] = connection.ConnectionToken;
                values["negotiateVersion"] = "1";
                var qs = new QueryCollection(values);
                context.Request.Query = qs;

                await dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), c => Task.CompletedTask);

                Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
            }
        }
    }

    [Fact]
    public async Task EndpointsThatRequireConnectionId400WhenNoConnectionIdProvidedForPost()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            using (var strm = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                context.Response.Body = strm;
                var services = new ServiceCollection();
                services.AddOptions();
                services.AddSingleton<TestConnectionHandler>();
                context.Request.Path = "/foo";
                context.Request.Method = "POST";
                context.Request.QueryString = new QueryString("?negotiateVersion=1");

                var builder = new ConnectionBuilder(services.BuildServiceProvider());
                builder.UseConnectionHandler<TestConnectionHandler>();
                var app = builder.Build();
                await dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);

                Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
                await strm.FlushAsync();
                Assert.Equal("Connection ID required", Encoding.UTF8.GetString(strm.ToArray()));
            }
        }
    }

    [Theory]
    [InlineData(HttpTransportType.LongPolling, 200)]
    [InlineData(HttpTransportType.WebSockets, 404)]
    [InlineData(HttpTransportType.ServerSentEvents, 404)]
    public async Task EndPointThatOnlySupportsLongPollingRejectsOtherTransports(HttpTransportType transportType, int status)
    {
        using (StartVerifiableLog())
        {
            await CheckTransportSupported(HttpTransportType.LongPolling, transportType, status, LoggerFactory);
        }
    }

    [Theory]
    [InlineData(HttpTransportType.ServerSentEvents, 200)]
    [InlineData(HttpTransportType.WebSockets, 404)]
    [InlineData(HttpTransportType.LongPolling, 404)]
    public async Task EndPointThatOnlySupportsSSERejectsOtherTransports(HttpTransportType transportType, int status)
    {
        using (StartVerifiableLog())
        {
            await CheckTransportSupported(HttpTransportType.ServerSentEvents, transportType, status, LoggerFactory);
        }
    }

    [Theory]
    [InlineData(HttpTransportType.WebSockets, 200)]
    [InlineData(HttpTransportType.ServerSentEvents, 404)]
    [InlineData(HttpTransportType.LongPolling, 404)]
    public async Task EndPointThatOnlySupportsWebSockesRejectsOtherTransports(HttpTransportType transportType, int status)
    {
        using (StartVerifiableLog())
        {
            await CheckTransportSupported(HttpTransportType.WebSockets, transportType, status, LoggerFactory);
        }
    }

    [Theory]
    [InlineData(HttpTransportType.LongPolling, 404)]
    public async Task EndPointThatOnlySupportsWebSocketsAndSSERejectsLongPolling(HttpTransportType transportType, int status)
    {
        using (StartVerifiableLog())
        {
            await CheckTransportSupported(HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents, transportType, status, LoggerFactory);
        }
    }

    [Fact]
    public async Task CompletedEndPointEndsConnection()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<ImmediatelyCompleteConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);

            SetTransport(context, HttpTransportType.ServerSentEvents);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<ImmediatelyCompleteConnectionHandler>();
            var app = builder.Build();
            await dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

            var exists = manager.TryGetConnection(connection.ConnectionToken, out _);
            Assert.False(exists);
        }
    }

    [Fact]
    public async Task SynchronousExceptionEndsConnection()
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == typeof(HttpConnectionManager).FullName &&
                   writeContext.EventId.Name == "FailedDispose";
        }

        using (StartVerifiableLog(expectedErrorsFilter: ExpectedErrors))
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();

            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = new ServiceCollection();
            services.AddSingleton<SynchronusExceptionConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);
            SetTransport(context, HttpTransportType.ServerSentEvents);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<SynchronusExceptionConnectionHandler>();
            var app = builder.Build();
            await dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

            var exists = manager.TryGetConnection(connection.ConnectionId, out _);
            Assert.False(exists);
        }
    }

    [Fact]
    public async Task CompletedEndPointEndsLongPollingConnection()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<ImmediatelyCompleteConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<ImmediatelyCompleteConnectionHandler>();
            var app = builder.Build();
            // First poll will 200
            await dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

            await dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);

            Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
            AssertResponseHasCacheHeaders(context.Response);

            var exists = manager.TryGetConnection(connection.ConnectionId, out _);
            Assert.False(exists);
        }
    }

    [Fact]
    public async Task Metrics()
    {
        using (StartVerifiableLog())
        {
            var testMeterFactory = new TestMeterFactory();
            using var connectionDuration = new MetricCollector<double>(testMeterFactory, HttpConnectionsMetrics.MeterName, "signalr.server.connection.duration");
            using var currentConnections = new MetricCollector<long>(testMeterFactory, HttpConnectionsMetrics.MeterName, "signalr.server.active_connections");

            var metrics = new HttpConnectionsMetrics(testMeterFactory);
            var manager = CreateConnectionManager(LoggerFactory, metrics);
            var connection = manager.CreateConnection();

            var dispatcher = CreateDispatcher(manager, LoggerFactory, metrics);

            var services = new ServiceCollection();
            services.AddSingleton<ImmediatelyCompleteConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<ImmediatelyCompleteConnectionHandler>();
            var app = builder.Build();
            // First poll will 200
            await dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

            await dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);

            Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
            AssertResponseHasCacheHeaders(context.Response);

            var exists = manager.TryGetConnection(connection.ConnectionId, out _);
            Assert.False(exists);

            Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => AssertDuration(m, "normal_closure", "long_polling"));
            Assert.Collection(currentConnections.GetMeasurementSnapshot(), m => AssertTransport(m, 1, "long_polling"), m => AssertTransport(m, -1, "long_polling"));
        }
    }

    [Fact]
    public async Task Metrics_ListenStartAfterConnection_Empty()
    {
        using (StartVerifiableLog())
        {
            var testMeterFactory = new TestMeterFactory();
            var metrics = new HttpConnectionsMetrics(testMeterFactory);
            var manager = CreateConnectionManager(LoggerFactory, metrics);
            var connection = manager.CreateConnection();

            var dispatcher = CreateDispatcher(manager, LoggerFactory, metrics);

            var services = new ServiceCollection();
            services.AddSingleton<ImmediatelyCompleteConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<ImmediatelyCompleteConnectionHandler>();
            var app = builder.Build();
            // First poll will 200
            await dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

            using var connectionDuration = new MetricCollector<double>(testMeterFactory, HttpConnectionsMetrics.MeterName, "signalr.server.connection.duration");
            using var currentConnections = new MetricCollector<long>(testMeterFactory, HttpConnectionsMetrics.MeterName, "signalr.server.active_connections");

            await dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);

            Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
            AssertResponseHasCacheHeaders(context.Response);

            var exists = manager.TryGetConnection(connection.ConnectionId, out _);
            Assert.False(exists);

            Assert.Empty(currentConnections.GetMeasurementSnapshot());
            Assert.Empty(connectionDuration.GetMeasurementSnapshot());
        }
    }

    private static void AssertTransport(CollectedMeasurement<long> measurement, long expected, string transportType)
    {
        Assert.Equal(expected, measurement.Value);
        Assert.Equal(transportType.ToString(), (string)measurement.Tags["signalr.transport"]);
    }

    private static void AssertDuration(CollectedMeasurement<double> measurement, string status, string transportType)
    {
        Assert.True(measurement.Value > 0);
        Assert.Equal(status.ToString(), (string)measurement.Tags["signalr.connection.status"]);
        Assert.Equal(transportType.ToString(), (string)measurement.Tags["signalr.transport"]);
    }

    [Fact]
    public async Task LongPollingTimeoutSets200StatusCode()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();
            options.LongPolling.PollTimeout = TimeSpan.FromSeconds(2);
            await dispatcher.ExecuteAsync(context, options, app).DefaultTimeout();

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            AssertResponseHasCacheHeaders(context.Response);
        }
    }

    private class BlockingStream : Stream
    {
        private readonly SyncPoint _sync;
        private bool _isSSE;
        public BlockingStream(SyncPoint sync, bool isSSE = false)
        {
            _sync = sync;
            _isSSE = isSSE;
        }
        public override bool CanRead => throw new NotImplementedException();
        public override bool CanSeek => throw new NotImplementedException();
        public override bool CanWrite => throw new NotImplementedException();
        public override long Length => throw new NotImplementedException();
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        public override void Flush()
        {
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_isSSE)
            {
                // SSE does an initial write of :\r\n that we want to ignore in testing
                _isSSE = false;
                return;
            }
            await _sync.WaitToContinue();
            cancellationToken.ThrowIfCancellationRequested();
        }
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_isSSE)
            {
                // SSE does an initial write of :\r\n that we want to ignore in testing
                _isSSE = false;
                return;
            }
            await _sync.WaitToContinue();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    [Fact]
    [LogLevel(LogLevel.Debug)]
    public async Task LongPollingConnectionClosesWhenSendTimeoutReached()
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return (writeContext.LoggerName == typeof(Internal.Transports.LongPollingServerTransport).FullName &&
                   writeContext.EventId.Name == "LongPollingTerminated") ||
                   (writeContext.LoggerName == typeof(HttpConnectionManager).FullName && writeContext.EventId.Name == "FailedDispose");
        }

        using (StartVerifiableLog(expectedErrorsFilter: ExpectedErrors))
        {
            var initialTime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);
            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            // First poll completes immediately
            var options = new HttpConnectionDispatcherOptions();
            await dispatcher.ExecuteAsync(context, options, app).DefaultTimeout();
            var sync = new SyncPoint();
            context.Response.Body = new BlockingStream(sync);
            var dispatcherTask = dispatcher.ExecuteAsync(context, options, app);
            await connection.Transport.Output.WriteAsync(new byte[] { 1 }).DefaultTimeout();
            await sync.WaitForSyncPoint().DefaultTimeout();

            // Try cancel before cancellation should occur
            connection.TryCancelSend(initialTime + options.TransportSendTimeout);
            Assert.False(connection.SendingToken.IsCancellationRequested);

            // Cancel write to response body
            connection.TryCancelSend(TimeSpan.FromMilliseconds(Environment.TickCount64) + options.TransportSendTimeout + TimeSpan.FromTicks(1));
            Assert.True(connection.SendingToken.IsCancellationRequested);

            sync.Continue();
            await dispatcherTask.DefaultTimeout();
            // Connection should be removed on canceled write
            Assert.False(manager.TryGetConnection(connection.ConnectionId, out var _));
        }
    }

    [Fact]
    [LogLevel(LogLevel.Debug)]
    public async Task SSEConnectionClosesWhenSendTimeoutReached()
    {
        using (StartVerifiableLog())
        {
            var initialTime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);
            SetTransport(context, HttpTransportType.ServerSentEvents);
            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            var sync = new SyncPoint();
            context.Response.Body = new BlockingStream(sync, isSSE: true);
            var options = new HttpConnectionDispatcherOptions();
            var dispatcherTask = dispatcher.ExecuteAsync(context, options, app);
            await connection.Transport.Output.WriteAsync(new byte[] { 1 }).DefaultTimeout();
            await sync.WaitForSyncPoint().DefaultTimeout();

            // Try cancel before cancellation should occur
            connection.TryCancelSend(initialTime + options.TransportSendTimeout);
            Assert.False(connection.SendingToken.IsCancellationRequested);

            // Cancel write to response body
            connection.TryCancelSend(TimeSpan.FromMilliseconds(Environment.TickCount64) + options.TransportSendTimeout + TimeSpan.FromTicks(1));
            Assert.True(connection.SendingToken.IsCancellationRequested);

            sync.Continue();
            await dispatcherTask.DefaultTimeout();
            // Connection should be removed on canceled write
            Assert.False(manager.TryGetConnection(connection.ConnectionId, out var _));
        }
    }

    [Fact]
    [LogLevel(LogLevel.Debug)]
    public async Task WebSocketConnectionClosesWhenSendTimeoutReached()
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == typeof(Internal.Transports.WebSocketsServerTransport).FullName &&
                   writeContext.EventId.Name == "ErrorWritingFrame";
        }
        using (StartVerifiableLog(expectedErrorsFilter: ExpectedErrors))
        {
            var initialTime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var sync = new SyncPoint();
            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);
            SetTransport(context, HttpTransportType.WebSockets, sync);
            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();
            options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(0);
            var dispatcherTask = dispatcher.ExecuteAsync(context, options, app);
            await connection.Transport.Output.WriteAsync(new byte[] { 1 }).DefaultTimeout();
            await sync.WaitForSyncPoint().DefaultTimeout();

            // Try cancel before cancellation should occur
            connection.TryCancelSend(initialTime + options.TransportSendTimeout);
            Assert.False(connection.SendingToken.IsCancellationRequested);

            // Cancel write to response body
            connection.TryCancelSend(TimeSpan.FromMilliseconds(Environment.TickCount64) + options.TransportSendTimeout + TimeSpan.FromTicks(1));
            Assert.True(connection.SendingToken.IsCancellationRequested);

            sync.Continue();
            await dispatcherTask.DefaultTimeout();
            // Connection should be removed on canceled write
            Assert.False(manager.TryGetConnection(connection.ConnectionId, out var _));
        }
    }

    [Fact]
    [LogLevel(LogLevel.Trace)]
    public async Task WebSocketTransportTimesOutWhenCloseFrameNotReceived()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.WebSockets;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<ImmediatelyCompleteConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);
            SetTransport(context, HttpTransportType.WebSockets);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<ImmediatelyCompleteConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();
            options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(1);

            var task = dispatcher.ExecuteAsync(context, options, app);

            await task.DefaultTimeout();
        }
    }

    [Theory]
    [InlineData(HttpTransportType.WebSockets)]
    [InlineData(HttpTransportType.ServerSentEvents)]
    public async Task RequestToActiveConnectionId409ForStreamingTransports(HttpTransportType transportType)
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var context1 = MakeRequest("/foo", connection, services);
            var context2 = MakeRequest("/foo", connection, services);

            SetTransport(context1, transportType);
            SetTransport(context2, transportType);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();
            var request1 = dispatcher.ExecuteAsync(context1, options, app);

            await dispatcher.ExecuteAsync(context2, options, app).DefaultTimeout();

            Assert.False(request1.IsCompleted);

            Assert.Equal(StatusCodes.Status409Conflict, context2.Response.StatusCode);
            Assert.NotSame(connection.HttpContext, context2);

            var webSocketTask = Task.CompletedTask;

            var ws = (TestWebSocketConnectionFeature)context1.Features.Get<IHttpWebSocketFeature>();
            if (ws != null)
            {
                await ws.Client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }

            manager.CloseConnections();

            await request1.DefaultTimeout();
        }
    }

    [Fact]
    public async Task RequestToActiveConnectionIdKillsPreviousConnectionLongPolling()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var context1 = MakeRequest("/foo", connection, services);
            var context2 = MakeRequest("/foo", connection, services);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();
            var request1 = dispatcher.ExecuteAsync(context1, options, app);
            Assert.True(request1.IsCompleted);

            request1 = dispatcher.ExecuteAsync(context1, options, app);
            var count = 0;
            // Wait until the request has started internally
            while (connection.TransportTask.IsCompleted && count < 50)
            {
                count++;
                await Task.Delay(15);
            }
            if (count == 50)
            {
                Assert.True(false, "Poll took too long to start");
            }

            var request2 = dispatcher.ExecuteAsync(context2, options, app);

            // Wait for poll to be canceled
            await request1.DefaultTimeout();

            Assert.Equal(StatusCodes.Status204NoContent, context1.Response.StatusCode);
            AssertResponseHasCacheHeaders(context1.Response);

            count = 0;
            // Wait until the second request has started internally
            while (connection.TransportTask.IsCompleted && count < 50)
            {
                count++;
                await Task.Delay(15);
            }
            if (count == 50)
            {
                Assert.True(false, "Poll took too long to start");
            }
            Assert.Equal(HttpConnectionStatus.Active, connection.Status);

            Assert.False(request2.IsCompleted);

            manager.CloseConnections();

            await request2;
        }
    }

    [Fact]
    public async Task MultipleRequestsToActiveConnectionId409ForLongPolling()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var context1 = MakeRequest("/foo", connection, services);
            var context2 = MakeRequest("/foo", connection, services);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();

            // Prime the polling. Expect any empty response showing the transport is initialized.
            var request1 = dispatcher.ExecuteAsync(context1, options, app);
            Assert.True(request1.IsCompleted);

            // Manually control PreviousPollTask instead of using a real PreviousPollTask, because a real
            // PreviousPollTask might complete too early when the second request cancels it.
            var lastPollTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            connection.PreviousPollTask = lastPollTcs.Task;

            request1 = dispatcher.ExecuteAsync(context1, options, app);
            var request2 = dispatcher.ExecuteAsync(context2, options, app);

            Assert.False(request1.IsCompleted);
            Assert.False(request2.IsCompleted);

            lastPollTcs.SetResult();

            var completedTask = await Task.WhenAny(request1, request2).DefaultTimeout();

            if (completedTask == request1)
            {
                Assert.Equal(StatusCodes.Status409Conflict, context1.Response.StatusCode);
                Assert.False(request2.IsCompleted);
                AssertResponseHasCacheHeaders(context1.Response);
            }
            else
            {
                Assert.Equal(StatusCodes.Status409Conflict, context2.Response.StatusCode);
                Assert.False(request1.IsCompleted);
                AssertResponseHasCacheHeaders(context2.Response);
            }

            Assert.Equal(HttpConnectionStatus.Active, connection.Status);

            manager.CloseConnections();

            await request1.DefaultTimeout();
            await request2.DefaultTimeout();
        }
    }

    [Theory]
    [InlineData(HttpTransportType.ServerSentEvents)]
    [InlineData(HttpTransportType.LongPolling)]
    public async Task RequestToDisposedConnectionIdReturns404(HttpTransportType transportType)
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.Status = HttpConnectionStatus.Disposed;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);
            SetTransport(context, transportType);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();
            await dispatcher.ExecuteAsync(context, options, app);

            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

            if (transportType == HttpTransportType.LongPolling)
            {
                AssertResponseHasCacheHeaders(context.Response);
            }
        }
    }

    [Fact]
    public async Task ConnectionStateSetToInactiveAfterPoll()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();
            var task = dispatcher.ExecuteAsync(context, options, app);

            var buffer = Encoding.UTF8.GetBytes("Hello World");

            // Write to the transport so the poll yields
            await connection.Transport.Output.WriteAsync(buffer);

            await task;

            Assert.Equal(HttpConnectionStatus.Inactive, connection.Status);
            Assert.NotNull(connection.GetHttpContext());

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }
    }

    [Fact]
    public async Task BlockingConnectionWorksWithStreamingConnections()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<BlockingConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);
            SetTransport(context, HttpTransportType.ServerSentEvents);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<BlockingConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();
            var task = dispatcher.ExecuteAsync(context, options, app);

            var buffer = Encoding.UTF8.GetBytes("Hello World");

            // Write to the application
            await connection.Application.Output.WriteAsync(buffer);

            await task;

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            var exists = manager.TryGetConnection(connection.ConnectionToken, out _);
            Assert.False(exists);
        }
    }

    [Fact]
    public async Task BlockingConnectionWorksWithLongPollingConnection()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<BlockingConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<BlockingConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();

            // Initial poll
            var task = dispatcher.ExecuteAsync(context, options, app);
            Assert.True(task.IsCompleted);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

            // Real long running poll
            task = dispatcher.ExecuteAsync(context, options, app);

            var buffer = Encoding.UTF8.GetBytes("Hello World");

            // Write to the application
            await connection.Application.Output.WriteAsync(buffer);

            await task;

            Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
            AssertResponseHasCacheHeaders(context.Response);
            var exists = manager.TryGetConnection(connection.ConnectionToken, out _);
            Assert.False(exists);
        }
    }

    [Fact]
    public async Task AttemptingToPollWhileAlreadyPollingReplacesTheCurrentPoll()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();

            var context1 = MakeRequest("/foo", connection, services);
            // This is the initial poll to make sure things are setup
            var task1 = dispatcher.ExecuteAsync(context1, options, app);
            Assert.True(task1.IsCompleted);
            task1 = dispatcher.ExecuteAsync(context1, options, app);
            var context2 = MakeRequest("/foo", connection, services);
            var task2 = dispatcher.ExecuteAsync(context2, options, app);

            // Task 1 should finish when request 2 arrives
            await task1.DefaultTimeout();

            // Send a message from the app to complete Task 2
            await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello, World"));

            await task2.DefaultTimeout();

            // Verify the results
            Assert.Equal(StatusCodes.Status204NoContent, context1.Response.StatusCode);
            Assert.Equal(string.Empty, GetContentAsString(context1.Response.Body));
            AssertResponseHasCacheHeaders(context1.Response);
            Assert.Equal(StatusCodes.Status200OK, context2.Response.StatusCode);
            Assert.Equal("Hello, World", GetContentAsString(context2.Response.Body));
            AssertResponseHasCacheHeaders(context2.Response);
        }
    }

    [Theory]
    [InlineData(HttpTransportType.LongPolling, null)]
    [InlineData(HttpTransportType.ServerSentEvents, TransferFormat.Text)]
    [InlineData(HttpTransportType.WebSockets, TransferFormat.Binary | TransferFormat.Text)]
    public async Task TransferModeSet(HttpTransportType transportType, TransferFormat? expectedTransferFormats)
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<ImmediatelyCompleteConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);
            SetTransport(context, transportType);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<ImmediatelyCompleteConnectionHandler>();
            var app = builder.Build();

            var options = new HttpConnectionDispatcherOptions();
            options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(0);
            await dispatcher.ExecuteAsync(context, options, app);

            if (expectedTransferFormats != null)
            {
                var transferFormatFeature = connection.Features.Get<ITransferFormatFeature>();
                Assert.Equal(expectedTransferFormats.Value, transferFormatFeature.SupportedFormats);
            }
        }
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public async Task LongPollingKeepsWindowsPrincipalAndIdentityBetweenRequests()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddSingleton<TestConnectionHandler>();
            services.AddLogging();
            var sp = services.BuildServiceProvider();
            context.Request.Path = "/foo";
            context.Request.Method = "GET";
            context.RequestServices = sp;
            var values = new Dictionary<string, StringValues>();
            values["id"] = connection.ConnectionToken;
            values["negotiateVersion"] = "1";
            var qs = new QueryCollection(values);
            context.Request.Query = qs;
            var builder = new ConnectionBuilder(sp);
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();

            var windowsIdentity = WindowsIdentity.GetAnonymous();
            context.User = new WindowsPrincipal(windowsIdentity);
            context.User.AddIdentity(new ClaimsIdentity());

            // would get stuck if EndPoint was running
            await dispatcher.ExecuteAsync(context, options, app).DefaultTimeout();

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            var currentUser = connection.User;

            var connectionHandlerTask = dispatcher.ExecuteAsync(context, options, app);
            await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Unblock")).AsTask().DefaultTimeout();
            await connectionHandlerTask.DefaultTimeout();

            // This is the important check
            Assert.Same(currentUser, connection.User);
            Assert.IsType<WindowsPrincipal>(currentUser);
            Assert.Equal(2, connection.User.Identities.Count());

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public async Task LongPollingKeepsWindowsIdentityWithoutWindowsPrincipalBetweenRequests()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddSingleton<TestConnectionHandler>();
            services.AddLogging();
            var sp = services.BuildServiceProvider();
            context.Request.Path = "/foo";
            context.Request.Method = "GET";
            context.RequestServices = sp;
            var values = new Dictionary<string, StringValues>();
            values["id"] = connection.ConnectionToken;
            values["negotiateVersion"] = "1";
            var qs = new QueryCollection(values);
            context.Request.Query = qs;
            var builder = new ConnectionBuilder(sp);
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();

            var windowsIdentity = WindowsIdentity.GetAnonymous();
            context.User = new ClaimsPrincipal(windowsIdentity);
            context.User.AddIdentity(new ClaimsIdentity());

            // would get stuck if EndPoint was running
            await dispatcher.ExecuteAsync(context, options, app).DefaultTimeout();

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            var currentUser = connection.User;

            var connectionHandlerTask = dispatcher.ExecuteAsync(context, options, app);
            await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Unblock")).AsTask().DefaultTimeout();
            await connectionHandlerTask.DefaultTimeout();

            // This is the important check
            Assert.Same(currentUser, connection.User);
            Assert.IsNotType<WindowsPrincipal>(currentUser);
            Assert.Equal(2, connection.User.Identities.Count());

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }
    }

    [ConditionalTheory]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    [InlineData(HttpTransportType.LongPolling)]
    [InlineData(HttpTransportType.ServerSentEvents)]
    [InlineData(HttpTransportType.WebSockets)]
    public async Task WindowsIdentityNotClosed(HttpTransportType transportType)
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = transportType;
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddSingleton<TestConnectionHandler>();
            services.AddLogging();

            var context = MakeRequest("/foo", connection, services);
            SetTransport(context, transportType);
            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<ImmediatelyCompleteConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();
            options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(0);

            var windowsIdentity = WindowsIdentity.GetAnonymous();
            context.User = new WindowsPrincipal(windowsIdentity);
            context.User.AddIdentity(new ClaimsIdentity());

            if (transportType == HttpTransportType.LongPolling)
            {
                // first poll effectively noops
                await dispatcher.ExecuteAsync(context, options, app).DefaultTimeout();
            }

            await dispatcher.ExecuteAsync(context, options, app).DefaultTimeout();

            // Identity shouldn't be closed by the connections layer
            Assert.False(windowsIdentity.AccessToken.IsClosed);

            if (transportType == HttpTransportType.LongPolling)
            {
                // Long polling clones the user, make sure it disposes it too
                Assert.True(((WindowsIdentity)connection.User.Identity).AccessToken.IsClosed);
            }
        }
    }

    [Fact]
    public async Task SetsInherentKeepAliveFeatureOnFirstLongPollingRequest()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();
            options.LongPolling.PollTimeout = TimeSpan.FromMilliseconds(1); // We don't care about the poll itself

            await dispatcher.ExecuteAsync(context, options, app).DefaultTimeout();

            Assert.True(connection.HasInherentKeepAlive);

            // Check via the feature as well to make sure it's there.
            Assert.True(connection.Features.Get<IConnectionInherentKeepAliveFeature>().HasInherentKeepAlive);
        }
    }

    [Theory]
    [InlineData(HttpTransportType.ServerSentEvents)]
    [InlineData(HttpTransportType.WebSockets)]
    public async Task DeleteEndpointRejectsRequestToTerminateNonLongPollingTransport(HttpTransportType transportType)
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = transportType;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<TestConnectionHandler>();
            var context = MakeRequest("/foo", connection, serviceCollection);
            SetTransport(context, transportType);

            var services = serviceCollection.BuildServiceProvider();
            var builder = new ConnectionBuilder(services);
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();

            _ = dispatcher.ExecuteAsync(context, options, app).DefaultTimeout();

            // Issue the delete request
            var deleteContext = new DefaultHttpContext();
            deleteContext.Request.Path = "/foo";
            deleteContext.Request.QueryString = new QueryString($"?id={connection.ConnectionToken}");
            deleteContext.Request.Method = "DELETE";
            var ms = new MemoryStream();
            deleteContext.Response.Body = ms;

            await dispatcher.ExecuteAsync(deleteContext, options, app).DefaultTimeout();

            // Verify the response from the DELETE request
            Assert.Equal(StatusCodes.Status400BadRequest, deleteContext.Response.StatusCode);
            Assert.Equal("text/plain", deleteContext.Response.ContentType);
            Assert.Equal("Cannot terminate this connection using the DELETE endpoint.", Encoding.UTF8.GetString(ms.ToArray()));
        }
    }

    [Fact]
    public async Task DeleteEndpointGracefullyTerminatesLongPolling()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();

            var pollTask = dispatcher.ExecuteAsync(context, options, app);
            Assert.True(pollTask.IsCompleted);

            // Now send the second poll
            pollTask = dispatcher.ExecuteAsync(context, options, app);

            // Issue the delete request and make sure the poll completes
            var deleteContext = new DefaultHttpContext();
            deleteContext.Request.Path = "/foo";
            deleteContext.Request.QueryString = new QueryString($"?id={connection.ConnectionToken}");
            deleteContext.Request.Method = "DELETE";

            Assert.False(pollTask.IsCompleted);

            await dispatcher.ExecuteAsync(deleteContext, options, app).DefaultTimeout();

            await pollTask.DefaultTimeout();

            // Verify that everything shuts down
            await connection.ApplicationTask.DefaultTimeout();
            await connection.TransportTask.DefaultTimeout();

            // Verify the response from the DELETE request
            Assert.Equal(StatusCodes.Status202Accepted, deleteContext.Response.StatusCode);
            Assert.Equal("text/plain", deleteContext.Response.ContentType);

            await connection.DisposeAndRemoveTask.DefaultTimeout();

            // Verify the connection was removed from the manager
            Assert.False(manager.TryGetConnection(connection.ConnectionToken, out _));
        }
    }

    [Fact]
    public async Task DeleteEndpointGracefullyTerminatesLongPollingEvenWhenBetweenPolls()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();
            options.LongPolling.PollTimeout = TimeSpan.FromMilliseconds(1);

            await dispatcher.ExecuteAsync(context, options, app).DefaultTimeout();

            // Issue the delete request and make sure the poll completes
            var deleteContext = new DefaultHttpContext();
            deleteContext.Request.Path = "/foo";
            deleteContext.Request.QueryString = new QueryString($"?id={connection.ConnectionToken}");
            deleteContext.Request.Method = "DELETE";

            await dispatcher.ExecuteAsync(deleteContext, options, app).DefaultTimeout();

            // Verify the response from the DELETE request
            Assert.Equal(StatusCodes.Status202Accepted, deleteContext.Response.StatusCode);
            Assert.Equal("text/plain", deleteContext.Response.ContentType);

            // Verify that everything shuts down
            await connection.ApplicationTask.DefaultTimeout();
            await connection.TransportTask.DefaultTimeout();

            Assert.NotNull(connection.DisposeAndRemoveTask);

            await connection.DisposeAndRemoveTask.DefaultTimeout();

            // Verify the connection was removed from the manager
            Assert.False(manager.TryGetConnection(connection.ConnectionToken, out _));
        }
    }

    [Fact]
    public async Task DeleteEndpointTerminatesLongPollingWithHangingApplication()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions
            {
                TransportMaxBufferSize = 2,
                ApplicationMaxBufferSize = 2
            };
            var connection = manager.CreateConnection(options);
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<NeverEndingConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<NeverEndingConnectionHandler>();
            var app = builder.Build();

            var pollTask = dispatcher.ExecuteAsync(context, options, app);
            Assert.True(pollTask.IsCompleted);

            // Now send the second poll
            pollTask = dispatcher.ExecuteAsync(context, options, app);

            // Issue the delete request and make sure the poll completes
            var deleteContext = new DefaultHttpContext();
            deleteContext.Request.Path = "/foo";
            deleteContext.Request.QueryString = new QueryString($"?id={connection.ConnectionId}");
            deleteContext.Request.Method = "DELETE";

            Assert.False(pollTask.IsCompleted);

            await dispatcher.ExecuteAsync(deleteContext, options, app).DefaultTimeout();

            await pollTask.DefaultTimeout();

            // Verify that transport shuts down
            await connection.TransportTask.DefaultTimeout();

            // Verify the response from the DELETE request
            Assert.Equal(StatusCodes.Status202Accepted, deleteContext.Response.StatusCode);
            Assert.Equal("text/plain", deleteContext.Response.ContentType);
            Assert.Equal(HttpConnectionStatus.Disposed, connection.Status);

            // Verify the connection not removed because application is hanging
            Assert.True(manager.TryGetConnection(connection.ConnectionId, out _));
        }
    }

    [Fact]
    public async Task PollCanReceiveFinalMessageAfterAppCompletes()
    {
        using (StartVerifiableLog())
        {
            var transportType = HttpTransportType.LongPolling;
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = transportType;

            var waitForMessageTcs1 = new TaskCompletionSource();
            var messageTcs1 = new TaskCompletionSource();
            var waitForMessageTcs2 = new TaskCompletionSource();
            var messageTcs2 = new TaskCompletionSource();
            ConnectionDelegate connectionDelegate = async c =>
            {
                await waitForMessageTcs1.Task.DefaultTimeout();
                await c.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Message1")).DefaultTimeout();
                messageTcs1.TrySetResult();
                await waitForMessageTcs2.Task.DefaultTimeout();
                await c.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Message2")).DefaultTimeout();
                messageTcs2.TrySetResult();
            };
            {
                var options = new HttpConnectionDispatcherOptions();
                var context = MakeRequest("/foo", connection, new ServiceCollection());
                await dispatcher.ExecuteAsync(context, options, connectionDelegate).DefaultTimeout();

                // second poll should have data
                waitForMessageTcs1.SetResult();
                await messageTcs1.Task.DefaultTimeout();

                var ms = new MemoryStream();
                context.Response.Body = ms;
                // Now send the second poll
                await dispatcher.ExecuteAsync(context, options, connectionDelegate).DefaultTimeout();
                Assert.Equal("Message1", Encoding.UTF8.GetString(ms.ToArray()));

                waitForMessageTcs2.SetResult();
                await messageTcs2.Task.DefaultTimeout();

                context = MakeRequest("/foo", connection, new ServiceCollection());
                ms.Seek(0, SeekOrigin.Begin);
                context.Response.Body = ms;
                // This is the third poll which gets the final message after the app is complete
                await dispatcher.ExecuteAsync(context, options, connectionDelegate).DefaultTimeout();
                Assert.Equal("Message2", Encoding.UTF8.GetString(ms.ToArray()));
            }
        }
    }

    [Fact]
    public async Task NegotiateDoesNotReturnWebSocketsWhenNotAvailable()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = new DefaultHttpContext();
            context.Features.Set<IHttpResponseFeature>(new ResponseFeature());
            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            services.AddOptions();
            var ms = new MemoryStream();
            context.Request.Path = "/foo";
            context.Request.Method = "POST";
            context.Response.Body = ms;
            context.Request.QueryString = new QueryString("?negotiateVersion=1");
            await dispatcher.ExecuteNegotiateAsync(context, new HttpConnectionDispatcherOptions { Transports = HttpTransportType.WebSockets });

            var negotiateResponse = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(ms.ToArray()));
            var availableTransports = (JArray)negotiateResponse["availableTransports"];

            Assert.Empty(availableTransports);
        }
    }

    [Fact]
    public async Task NegotiateDoesNotReturnUseStatefulReconnectWhenNotEnabledOnServer()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = new DefaultHttpContext();
            context.Features.Set<IHttpResponseFeature>(new ResponseFeature());
            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            services.AddOptions();
            var ms = new MemoryStream();
            context.Request.Path = "/foo";
            context.Request.Method = "POST";
            context.Response.Body = ms;
            context.Request.QueryString = new QueryString("?negotiateVersion=1&UseStatefulReconnect=true");
            await dispatcher.ExecuteNegotiateAsync(context, new HttpConnectionDispatcherOptions { AllowStatefulReconnects = false });

            var negotiateResponse = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(ms.ToArray()));
            Assert.False(negotiateResponse.TryGetValue("useStatefulReconnect", out _));

            Assert.True(manager.TryGetConnection(negotiateResponse["connectionToken"].ToString(), out var connection));
#pragma warning disable CA2252 // This API requires opting into preview features
            Assert.Null(connection.Features.Get<IStatefulReconnectFeature>());
#pragma warning restore CA2252 // This API requires opting into preview features
        }
    }

    [Fact]
    public async Task NegotiateDoesNotReturnUseStatefulReconnectWhenEnabledOnServerButNotRequestedByClient()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = new DefaultHttpContext();
            context.Features.Set<IHttpResponseFeature>(new ResponseFeature());
            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            services.AddOptions();
            var ms = new MemoryStream();
            context.Request.Path = "/foo";
            context.Request.Method = "POST";
            context.Response.Body = ms;
            context.Request.QueryString = new QueryString("?negotiateVersion=1");
            await dispatcher.ExecuteNegotiateAsync(context, new HttpConnectionDispatcherOptions { AllowStatefulReconnects = true });

            var negotiateResponse = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(ms.ToArray()));
            Assert.False(negotiateResponse.TryGetValue("useStatefulReconnect", out _));

            Assert.True(manager.TryGetConnection(negotiateResponse["connectionToken"].ToString(), out var connection));
#pragma warning disable CA2252 // This API requires opting into preview features
            Assert.Null(connection.Features.Get<IStatefulReconnectFeature>());
#pragma warning restore CA2252 // This API requires opting into preview features
        }
    }

    [Fact]
    public async Task NegotiateReturnsUseStatefulReconnectWhenEnabledOnServerAndRequestedByClient()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = new DefaultHttpContext();
            context.Features.Set<IHttpResponseFeature>(new ResponseFeature());
            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            services.AddOptions();
            var ms = new MemoryStream();
            context.Request.Path = "/foo";
            context.Request.Method = "POST";
            context.Response.Body = ms;
            context.Request.QueryString = new QueryString("?negotiateVersion=1&UseStatefulReconnect=true");
            await dispatcher.ExecuteNegotiateAsync(context, new HttpConnectionDispatcherOptions { AllowStatefulReconnects = true });

            var negotiateResponse = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(ms.ToArray()));
            Assert.True((bool)negotiateResponse["useStatefulReconnect"]);

            Assert.True(manager.TryGetConnection(negotiateResponse["connectionToken"].ToString(), out var connection));
#pragma warning disable CA2252 // This API requires opting into preview features
            Assert.NotNull(connection.Features.Get<IStatefulReconnectFeature>());
#pragma warning restore CA2252 // This API requires opting into preview features
        }
    }

    [Fact]
    public async Task ReconnectStopsPreviousConnection()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions() { AllowStatefulReconnects = true };
            options.WebSockets.CloseTimeout = TimeSpan.FromMilliseconds(1);
            // pretend negotiate occurred
            var connection = manager.CreateConnection(options, negotiateVersion: 1, useStatefulReconnect: true);
            connection.TransportType = HttpTransportType.WebSockets;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = new ServiceCollection();

            var context = MakeRequest("/foo", connection, services);
            SetTransport(context, HttpTransportType.WebSockets);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<ReconnectConnectionHandler>();
            var app = builder.Build();

            var initialWebSocketTask = dispatcher.ExecuteAsync(context, options, app);

#pragma warning disable CA2252 // This API requires opting into preview features
            var reconnectFeature = connection.Features.Get<IStatefulReconnectFeature>();
#pragma warning restore CA2252 // This API requires opting into preview features
            Assert.NotNull(reconnectFeature);

            var firstMsg = new byte[] { 1, 4, 8, 9 };
            await connection.Application.Output.WriteAsync(firstMsg);

            var websocketFeature = (TestWebSocketConnectionFeature)context.Features.Get<IHttpWebSocketFeature>();
            await websocketFeature.Accepted.DefaultTimeout();
            // Run the client socket
            var webSocketMessage = await websocketFeature.Client.GetNextMessageAsync().DefaultTimeout();

            Assert.Equal(firstMsg, webSocketMessage.Buffer);

            var calledOnReconnectedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
#pragma warning disable CA2252 // This API requires opting into preview features
            reconnectFeature.OnReconnected((writer) =>
            {
                calledOnReconnectedTcs.SetResult();
                return Task.CompletedTask;
            });
#pragma warning restore CA2252 // This API requires opting into preview features

            // New websocket connection with previous connection token
            context = MakeRequest("/foo", connection, services);
            SetTransport(context, HttpTransportType.WebSockets);

            var newWebSocketTask = dispatcher.ExecuteAsync(context, options, app);

            // New connection with same token will complete previous request
            await initialWebSocketTask.DefaultTimeout();

            await calledOnReconnectedTcs.Task.DefaultTimeout();

            Assert.False(newWebSocketTask.IsCompleted);

            var secondMsg = new byte[] { 7, 6, 3, 2 };
            await connection.Application.Output.WriteAsync(secondMsg);

            websocketFeature = (TestWebSocketConnectionFeature)context.Features.Get<IHttpWebSocketFeature>();
            await websocketFeature.Accepted.DefaultTimeout();
            webSocketMessage = await websocketFeature.Client.GetNextMessageAsync().DefaultTimeout();
            Assert.Equal(secondMsg, webSocketMessage.Buffer);

            connection.Abort();

            await newWebSocketTask.DefaultTimeout();
        }
    }

    [Fact]
    public async Task DisableReconnectDisallowsReplacementConnection()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions() { AllowStatefulReconnects = true };
            options.WebSockets.CloseTimeout = TimeSpan.FromMilliseconds(1);
            // pretend negotiate occurred
            var connection = manager.CreateConnection(options, negotiateVersion: 1, useStatefulReconnect: true);

            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = new ServiceCollection();

            var context = MakeRequest("/foo", connection, services);
            SetTransport(context, HttpTransportType.WebSockets);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<ReconnectConnectionHandler>();
            var app = builder.Build();

            var initialWebSocketTask = dispatcher.ExecuteAsync(context, options, app);

#pragma warning disable CA2252 // This API requires opting into preview features
            var reconnectFeature = connection.Features.Get<IStatefulReconnectFeature>();
#pragma warning restore CA2252 // This API requires opting into preview features
            Assert.NotNull(reconnectFeature);

            var firstMsg = new byte[] { 1, 4, 8, 9 };
            await connection.Application.Output.WriteAsync(firstMsg);

            var websocketFeature = (TestWebSocketConnectionFeature)context.Features.Get<IHttpWebSocketFeature>();
            await websocketFeature.Accepted.DefaultTimeout();
            // Run the client socket
            var webSocketMessage = await websocketFeature.Client.GetNextMessageAsync().DefaultTimeout();

            Assert.Equal(firstMsg, webSocketMessage.Buffer);

            var called = false;
#pragma warning disable CA2252 // This API requires opting into preview features
            reconnectFeature.OnReconnected((writer) =>
            {
                called = true;
                return Task.CompletedTask;
            });
#pragma warning restore CA2252 // This API requires opting into preview features

            // Disable will not allow new connection to override existing
#pragma warning disable CA2252 // This API requires opting into preview features
            reconnectFeature.DisableReconnect();
#pragma warning restore CA2252 // This API requires opting into preview features

            // New websocket connection with previous connection token
            context = MakeRequest("/foo", connection, services);
            SetTransport(context, HttpTransportType.WebSockets);

            await dispatcher.ExecuteAsync(context, options, app).DefaultTimeout();

            Assert.Equal(409, context.Response.StatusCode);

            Assert.False(called);

            // Connection still works
            var secondMsg = new byte[] { 7, 6, 3, 2 };
            await connection.Application.Output.WriteAsync(secondMsg);

            webSocketMessage = await websocketFeature.Client.GetNextMessageAsync().DefaultTimeout();
            Assert.Equal(secondMsg, webSocketMessage.Buffer);

            connection.Abort();

            await initialWebSocketTask.DefaultTimeout();
        }
    }

    private class ControllableMemoryStream : MemoryStream
    {
        private readonly SyncPoint _syncPoint;

        public ControllableMemoryStream(SyncPoint syncPoint)
        {
            _syncPoint = syncPoint;
        }

        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            await _syncPoint.WaitToContinue();

            await base.CopyToAsync(destination, bufferSize, cancellationToken);
        }
    }

    [Fact]
    public async Task WriteThatIsDisposedBeforeCompleteReturns404()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions
            {
                TransportMaxBufferSize = 13,
                ApplicationMaxBufferSize = 13
            };

            var connection = manager.CreateConnection(options);
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();

            SyncPoint streamCopySyncPoint = new SyncPoint();

            using (var responseBody = new MemoryStream())
            using (var requestBody = new ControllableMemoryStream(streamCopySyncPoint))
            {
                var context = new DefaultHttpContext();
                context.Request.Body = requestBody;
                context.Response.Body = responseBody;
                context.Request.Path = "/foo";
                context.Request.Method = "POST";
                var values = new Dictionary<string, StringValues>();
                values["id"] = connection.ConnectionToken;
                values["negotiateVersion"] = "1";
                var qs = new QueryCollection(values);
                context.Request.Query = qs;
                var buffer = Encoding.UTF8.GetBytes("Hello, world");
                requestBody.Write(buffer, 0, buffer.Length);
                requestBody.Seek(0, SeekOrigin.Begin);

                // Write
                var sendTask = dispatcher.ExecuteAsync(context, options, app);

                // Wait on the sync point inside ApplicationStream.CopyToAsync
                await streamCopySyncPoint.WaitForSyncPoint();

                // Start disposing. This will close the output and cause the write to error
                var disposeTask = connection.DisposeAsync().DefaultTimeout();

                // Continue writing on a completed writer
                streamCopySyncPoint.Continue();

                await sendTask.DefaultTimeout();
                await disposeTask.DefaultTimeout();

                // Ensure response status is correctly set
                Assert.Equal(404, context.Response.StatusCode);
            }
        }
    }

    [Fact]
    public async Task CanDisposeWhileWriteLockIsBlockedOnBackpressureAndResponseReturns404()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions
            {
                TransportMaxBufferSize = 13,
                ApplicationMaxBufferSize = 13
            };
            var connection = manager.CreateConnection(options);
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();

            using (var responseBody = new MemoryStream())
            using (var requestBody = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                context.Request.Body = requestBody;
                context.Response.Body = responseBody;
                context.Request.Path = "/foo";
                context.Request.Method = "POST";
                var values = new Dictionary<string, StringValues>();
                values["id"] = connection.ConnectionToken;
                values["negotiateVersion"] = "1";
                var qs = new QueryCollection(values);
                context.Request.Query = qs;
                var buffer = Encoding.UTF8.GetBytes("Hello, world");
                requestBody.Write(buffer, 0, buffer.Length);
                requestBody.Seek(0, SeekOrigin.Begin);

                // Write some data to the pipe to fill it up and make the next write wait
                await connection.ApplicationStream.WriteAsync(buffer, 0, buffer.Length).DefaultTimeout();

                // Write. This will take the WriteLock and block because of back pressure
                var sendTask = dispatcher.ExecuteAsync(context, options, app);

                // Start disposing. This will take the StateLock and attempt to take the WriteLock
                // Dispose will cancel pending flush and should unblock WriteLock
                await connection.DisposeAsync().DefaultTimeout();

                // Sends were unblocked
                await sendTask.DefaultTimeout();

                Assert.Equal(404, context.Response.StatusCode);
            }
        }
    }

    [Fact]
    public async Task LongPollingCanPollIfWritePipeHasBackpressure()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions
            {
                TransportMaxBufferSize = 13,
                ApplicationMaxBufferSize = 13
            };
            var connection = manager.CreateConnection(options);
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();

            using (var responseBody = new MemoryStream())
            using (var requestBody = new MemoryStream())
            {
                var context = new DefaultHttpContext();
                context.Request.Body = requestBody;
                context.Response.Body = responseBody;
                context.Request.Path = "/foo";
                context.Request.Method = "POST";
                var values = new Dictionary<string, StringValues>();
                values["id"] = connection.ConnectionToken;
                values["negotiateVersion"] = "1";
                var qs = new QueryCollection(values);
                context.Request.Query = qs;
                var buffer = Encoding.UTF8.GetBytes("Hello, world");
                requestBody.Write(buffer, 0, buffer.Length);
                requestBody.Seek(0, SeekOrigin.Begin);

                // Write some data to the pipe to fill it up and make the next write wait
                await connection.ApplicationStream.WriteAsync(buffer, 0, buffer.Length).DefaultTimeout();

                // This will block until the pipe is unblocked
                var sendTask = dispatcher.ExecuteAsync(context, options, app).DefaultTimeout();
                Assert.False(sendTask.IsCompleted);

                var pollContext = MakeRequest("/foo", connection, services);
                // This should unblock the send that is waiting because of backpressure
                // Testing deadlock regression where pipe backpressure would hold the same lock that poll would use
                await dispatcher.ExecuteAsync(pollContext, options, app).DefaultTimeout();

                await sendTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task ErrorDuringPollWillCloseConnection()
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return (writeContext.LoggerName.Equals("Microsoft.AspNetCore.Http.Connections.Internal.Transports.LongPollingTransport") &&
                   writeContext.EventId.Name == "LongPollingTerminated") ||
                   (writeContext.LoggerName == typeof(HttpConnectionManager).FullName &&
                   writeContext.EventId.Name == "FailedDispose");
        }

        using (StartVerifiableLog(expectedErrorsFilter: ExpectedErrors))
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();

            var context = MakeRequest("/foo", connection, services);

            // Initial poll will complete immediately
            await dispatcher.ExecuteAsync(context, options, app).DefaultTimeout();

            var pollContext = MakeRequest("/foo", connection, services);
            var pollTask = dispatcher.ExecuteAsync(pollContext, options, app);
            // fail LongPollingTransport ReadAsync
            connection.Transport.Output.Complete(new InvalidOperationException());
            await pollTask.DefaultTimeout();

            Assert.Equal(StatusCodes.Status500InternalServerError, pollContext.Response.StatusCode);
            Assert.False(manager.TryGetConnection(connection.ConnectionToken, out var _));
            AssertResponseHasCacheHeaders(pollContext.Response);
        }
    }

    [Fact]
    public async Task LongPollingConnectionClosingTriggersConnectionClosedToken()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions
            {
                TransportMaxBufferSize = 2,
                ApplicationMaxBufferSize = 2
            };
            var connection = manager.CreateConnection(options);
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<NeverEndingConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<NeverEndingConnectionHandler>();
            var app = builder.Build();

            var pollTask = dispatcher.ExecuteAsync(context, options, app);
            Assert.True(pollTask.IsCompleted);

            // Now send the second poll
            pollTask = dispatcher.ExecuteAsync(context, options, app);

            // Issue the delete request and make sure the poll completes
            var deleteContext = new DefaultHttpContext();
            deleteContext.Request.Path = "/foo";
            deleteContext.Request.QueryString = new QueryString($"?id={connection.ConnectionId}");
            deleteContext.Request.Method = "DELETE";

            Assert.False(pollTask.IsCompleted);

            await dispatcher.ExecuteAsync(deleteContext, options, app).DefaultTimeout();

            await pollTask.DefaultTimeout();

            // Verify that transport shuts down
            await connection.TransportTask.DefaultTimeout();

            // Verify the response from the DELETE request
            Assert.Equal(StatusCodes.Status202Accepted, deleteContext.Response.StatusCode);
            Assert.Equal("text/plain", deleteContext.Response.ContentType);
            Assert.Equal(HttpConnectionStatus.Disposed, connection.Status);

            await connection.ConnectionClosed.WaitForCancellationAsync().DefaultTimeout();

            // Verify the connection not removed because application is hanging
            Assert.True(manager.TryGetConnection(connection.ConnectionId, out _));
        }
    }

    [Fact]
    public async Task SSEConnectionClosingTriggersConnectionClosedToken()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = new ServiceCollection();
            services.AddSingleton<NeverEndingConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);
            SetTransport(context, HttpTransportType.ServerSentEvents);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<NeverEndingConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();
            _ = dispatcher.ExecuteAsync(context, options, app);

            // Close the SSE connection
            connection.Transport.Output.Complete();

            await connection.ConnectionClosed.WaitForCancellationAsync().DefaultTimeout();
        }
    }

    [Fact]
    public async Task WebSocketConnectionClosingTriggersConnectionClosedToken()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();

            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = new ServiceCollection();
            services.AddSingleton<NeverEndingConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);
            SetTransport(context, HttpTransportType.WebSockets);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<NeverEndingConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();
            options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(1);

            _ = dispatcher.ExecuteAsync(context, options, app);

            var websocket = (TestWebSocketConnectionFeature)context.Features.Get<IHttpWebSocketFeature>();
            await websocket.Accepted.DefaultTimeout();
            await websocket.Client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken: default).DefaultTimeout();

            await connection.ConnectionClosed.WaitForCancellationAsync().DefaultTimeout();
        }
    }

    [Fact]
    public async Task ServerClosingClosesWebSocketConnection()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();

            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = new ServiceCollection();
            services.AddSingleton<TestConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);
            SetTransport(context, HttpTransportType.WebSockets);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<TestConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();
            options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(1);

            var executeTask = dispatcher.ExecuteAsync(context, options, app);

            // "close" server, since we're not using a server in these tests we just simulate what would be called when the server closes
            await connection.DisposeAsync().DefaultTimeout();

            await connection.ConnectionClosed.WaitForCancellationAsync().DefaultTimeout();

            await executeTask.DefaultTimeout();
        }
    }

    public class CustomHttpRequestLifetimeFeature : IHttpRequestLifetimeFeature
    {
        public CancellationToken RequestAborted { get; set; }

        private readonly CancellationTokenSource _cts;
        public CustomHttpRequestLifetimeFeature()
        {
            _cts = new CancellationTokenSource();
            RequestAborted = _cts.Token;
        }

        public void Abort()
        {
            _cts.Cancel();
        }
    }

    [Fact]
    public async Task AbortingConnectionAbortsHttpContextAndTriggersConnectionClosedToken()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = new ServiceCollection();
            services.AddSingleton<NeverEndingConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);
            var lifetimeFeature = new CustomHttpRequestLifetimeFeature();
            context.Features.Set<IHttpRequestLifetimeFeature>(lifetimeFeature);
            SetTransport(context, HttpTransportType.ServerSentEvents);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<NeverEndingConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();
            _ = dispatcher.ExecuteAsync(context, options, app);

            connection.Abort();

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            connection.ConnectionClosed.Register(() => tcs.SetResult());
            await tcs.Task.DefaultTimeout();

            tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lifetimeFeature.RequestAborted.Register(() => tcs.SetResult());
            await tcs.Task.DefaultTimeout();
        }
    }

    [Fact]
    public async Task ServicesAvailableWithLongPolling()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<ServiceProviderConnectionHandler>();
            services.AddSingleton(new MessageWrapper() { Buffer = new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 }) });
            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<ServiceProviderConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();

            var context = MakeRequest("/foo", connection, services);

            // Initial poll will complete immediately
            await dispatcher.ExecuteAsync(context, options, app).DefaultTimeout();

            var pollContext = MakeRequest("/foo", connection, services);
            var pollTask = dispatcher.ExecuteAsync(pollContext, options, app);

            await connection.Application.Output.WriteAsync(new byte[] { 1 }).DefaultTimeout();
            await pollTask.DefaultTimeout();

            var memory = new Memory<byte>(new byte[10]);
            pollContext.Response.Body.Position = 0;
            Assert.Equal(3, await pollContext.Response.Body.ReadAsync(memory).DefaultTimeout());
            Assert.Equal(new byte[] { 1, 2, 3 }, memory.Slice(0, 3).ToArray());

            // Connection will use the original service provider so this will have no effect
            services.AddSingleton(new MessageWrapper() { Buffer = new ReadOnlySequence<byte>(new byte[] { 4, 5, 6 }) });
            pollContext = MakeRequest("/foo", connection, services);
            pollTask = dispatcher.ExecuteAsync(pollContext, options, app);

            await connection.Application.Output.WriteAsync(new byte[] { 1 }).DefaultTimeout();
            await pollTask.DefaultTimeout();

            pollContext.Response.Body.Position = 0;
            Assert.Equal(3, await pollContext.Response.Body.ReadAsync(memory).DefaultTimeout());
            Assert.Equal(new byte[] { 1, 2, 3 }, memory.Slice(0, 3).ToArray());

            await connection.DisposeAsync().DefaultTimeout();
        }
    }

    [Fact]
    public async Task ServicesPreserveScopeWithLongPolling()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<ServiceProviderConnectionHandler>();
            var iteration = 0;
            services.AddScoped(typeof(MessageWrapper), _ =>
            {
                iteration++;
                return new MessageWrapper() { Buffer = new ReadOnlySequence<byte>(new byte[] { (byte)(iteration + 1), (byte)(iteration + 2), (byte)(iteration + 3) }) };
            });

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<ServiceProviderConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();

            var context = MakeRequest("/foo", connection, services);

            // Initial poll will complete immediately
            await dispatcher.ExecuteAsync(context, options, app).DefaultTimeout();

            var pollContext = MakeRequest("/foo", connection, services);
            var pollTask = dispatcher.ExecuteAsync(pollContext, options, app);

            await connection.Application.Output.WriteAsync(new byte[] { 1 }).DefaultTimeout();
            await pollTask.DefaultTimeout();

            var memory = new Memory<byte>(new byte[10]);
            pollContext.Response.Body.Position = 0;
            Assert.Equal(3, await pollContext.Response.Body.ReadAsync(memory).DefaultTimeout());
            Assert.Equal(new byte[] { 2, 3, 4 }, memory.Slice(0, 3).ToArray());

            pollContext = MakeRequest("/foo", connection, services);
            pollTask = dispatcher.ExecuteAsync(pollContext, options, app);

            await connection.Application.Output.WriteAsync(new byte[] { 1 }).DefaultTimeout();
            await pollTask.DefaultTimeout();

            pollContext.Response.Body.Position = 0;
            Assert.Equal(3, await pollContext.Response.Body.ReadAsync(memory).DefaultTimeout());
            Assert.Equal(new byte[] { 2, 3, 4 }, memory.Slice(0, 3).ToArray());

            await connection.DisposeAsync().DefaultTimeout();
        }
    }

    [Fact]
    public async Task DisposeLongPollingConnectionDisposesServiceScope()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;

            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var services = new ServiceCollection();
            services.AddSingleton<ServiceProviderConnectionHandler>();
            var iteration = 0;
            services.AddScoped(typeof(MessageWrapper), _ =>
            {
                iteration++;
                return new MessageWrapper() { Buffer = new ReadOnlySequence<byte>(new byte[] { (byte)(iteration + 1), (byte)(iteration + 2), (byte)(iteration + 3) }) };
            });

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<ServiceProviderConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();

            var context = MakeRequest("/foo", connection, services);

            // Initial poll will complete immediately
            await dispatcher.ExecuteAsync(context, options, app).DefaultTimeout();

            // ServiceScope will be disposed here
            await connection.DisposeAsync().DefaultTimeout();

            Assert.Throws<ObjectDisposedException>(() => connection.ServiceScope.Value.ServiceProvider.GetService<MessageWrapper>());
        }
    }

    private class TestActivityFeature : IHttpActivityFeature
    {
        public TestActivityFeature(Activity activity)
        {
            Activity = activity;
        }

        public Activity Activity { get; set; }
    }

    [Fact]
    public async Task LongRunningActivityTagSetOnExecuteAsync()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();

            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = new ServiceCollection();
            services.AddSingleton<NeverEndingConnectionHandler>();
            var context = MakeRequest("/foo", connection, services);
            var cts = new CancellationTokenSource();
            context.RequestAborted = cts.Token;
            SetTransport(context, HttpTransportType.ServerSentEvents);

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<NeverEndingConnectionHandler>();
            var app = builder.Build();

            var activityFeature = new TestActivityFeature(new Activity("name"));
            activityFeature.Activity.Start();
            context.Features.Set<IHttpActivityFeature>(activityFeature);

            _ = dispatcher.ExecuteAsync(context, new HttpConnectionDispatcherOptions(), app);

            Assert.Equal("true", Activity.Current.GetTagItem("http.long_running"));

            connection.Transport.Output.Complete();

            await connection.ConnectionClosed.WaitForCancellationAsync().DefaultTimeout();

            activityFeature.Activity.Dispose();
        }
    }

    [Fact]
    public async Task ConnectionClosedRequestedTriggeredOnAuthExpiration()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory, TimeSpan.FromSeconds(5));
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var options = new HttpConnectionDispatcherOptions() { CloseOnAuthenticationExpiration = true };
            var connection = manager.CreateConnection(options);
            connection.TransportType = HttpTransportType.LongPolling;
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            connection.ConnectionClosedRequested.Register(() => tcs.SetResult());

            var services = new ServiceCollection();
            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<HttpContextConnectionHandler>();
            var app = builder.Build();
            var context = MakeRequest("/foo", connection, services);

            // Initial poll will complete immediately
            await dispatcher.ExecuteAsync(context, options, app).DefaultTimeout();

            var pollTask = dispatcher.ExecuteAsync(context, options, app);

            // AuthorizationExpiration is in the future so the scan won't do anything
            manager.Scan();

            await connection.Application.Output.WriteAsync(new byte[] { 1 }).DefaultTimeout();
            await pollTask.DefaultTimeout();

            var memory = new Memory<byte>(new byte[10]);
            context.Response.Body.Position = 0;
            Assert.Equal(1, await context.Response.Body.ReadAsync(memory).DefaultTimeout());
            Assert.Equal(new byte[] { 1 }, memory.Slice(0, 1).ToArray());

            context = MakeRequest("/foo", connection, services);
            pollTask = dispatcher.ExecuteAsync(context, options, app);

            // Set auth to an expired time
            connection.AuthenticationExpiration = DateTimeOffset.Now.AddSeconds(-1);

            manager.Scan();

            await tcs.Task.DefaultTimeout();

            await connection.DisposeAsync().DefaultTimeout();
        }
    }

    [Theory]
    [InlineData(HttpTransportType.LongPolling)]
    [InlineData(HttpTransportType.ServerSentEvents)]
    [InlineData(HttpTransportType.WebSockets)]
    public async Task AuthenticationExpirationSetOnAuthenticatedConnectionWithJWT(HttpTransportType transportType)
    {
        SymmetricSecurityKey SecurityKey = new SymmetricSecurityKey(SHA256.HashData(Guid.NewGuid().ToByteArray()));
        JwtSecurityTokenHandler JwtTokenHandler = new JwtSecurityTokenHandler();

        using var host = CreateHost(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        LifetimeValidator = (before, expires, token, parameters) => expires > DateTime.UtcNow,
                        ValidateAudience = false,
                        ValidateIssuer = false,
                        ValidateActor = false,
                        ValidateLifetime = true,
                        IssuerSigningKey = SecurityKey
                    };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        if (!string.IsNullOrEmpty(accessToken) &&
                            (context.HttpContext.WebSockets.IsWebSocketRequest || context.Request.Headers["Accept"] == "text/event-stream"))
                        {
                            context.Token = context.Request.Query["access_token"];
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        }, endpoints =>
        {
            endpoints.MapConnectionHandler<AuthConnectionHandler>("/foo", o => o.CloseOnAuthenticationExpiration = true);

            endpoints.MapGet("/generatetoken", context =>
            {
                return context.Response.WriteAsync(GenerateToken(context));
            });

            string GenerateToken(HttpContext httpContext)
            {
                var claims = new[] { new Claim(ClaimTypes.NameIdentifier, httpContext.Request.Query["user"]) };
                var credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
                var token = new JwtSecurityToken("SignalRTestServer", "SignalRTests", claims, expires: DateTime.UtcNow.AddMinutes(1), signingCredentials: credentials);
                return JwtTokenHandler.WriteToken(token);
            }
        }, LoggerFactory);

        host.Start();

        var manager = host.Services.GetRequiredService<HttpConnectionManager>();
        var url = host.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.Single();

        string token = "";
        using (var client = new HttpClient())
        {
            client.BaseAddress = new Uri(url);

            var response = await client.GetAsync("generatetoken?user=bob");
            token = await response.Content.ReadAsStringAsync();
        }

        url += "/foo";
        var stream = new MemoryStream();
        var connection = new HttpConnection(
            new HttpConnectionOptions()
            {
                Url = new Uri(url),
                AccessTokenProvider = () => Task.FromResult(token),
                Transports = transportType,
                DefaultTransferFormat = TransferFormat.Text,
                HttpMessageHandlerFactory = handler => new GetNegotiateHttpHandler(handler, stream)
            },
            LoggerFactory);

        await connection.StartAsync();

        var negotiateResponse = NegotiateProtocol.ParseResponse(stream.ToArray());

        Assert.True(manager.TryGetConnection(negotiateResponse.ConnectionToken, out var context));

        Assert.True(context.AuthenticationExpiration > DateTimeOffset.UtcNow);
        Assert.True(context.AuthenticationExpiration < DateTimeOffset.MaxValue);

        await connection.DisposeAsync();
    }

    [Theory]
    [InlineData(HttpTransportType.LongPolling)]
    [InlineData(HttpTransportType.ServerSentEvents)]
    [InlineData(HttpTransportType.WebSockets)]
    public async Task AuthenticationExpirationSetOnAuthenticatedConnectionWithCookies(HttpTransportType transportType)
    {
        using var host = CreateHost(services =>
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie();
        }, endpoints =>
        {
            endpoints.MapConnectionHandler<AuthConnectionHandler>("/foo", o => o.CloseOnAuthenticationExpiration = true);

            endpoints.MapGet("/signin", async context =>
            {
                var claims = new List<Claim>
                {
                        new Claim(ClaimTypes.NameIdentifier, context.Request.Query["user"])
                };
                await context.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies")));
            });
        }, LoggerFactory);

        host.Start();

        var manager = host.Services.GetRequiredService<HttpConnectionManager>();
        var url = host.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.Single();

        var cookies = new CookieContainer();
        using (var client = new HttpClient(new HttpClientHandler() { CookieContainer = cookies }))
        {
            client.BaseAddress = new Uri(url);

            var response = await client.GetAsync("signin?user=bob");
        }

        url += "/foo";
        var stream = new MemoryStream();
        var connection = new HttpConnection(
            new HttpConnectionOptions()
            {
                Url = new Uri(url),
                Transports = transportType,
                DefaultTransferFormat = TransferFormat.Text,
                HttpMessageHandlerFactory = handler => new GetNegotiateHttpHandler(handler, stream),
                Cookies = cookies
            },
            LoggerFactory);

        await connection.StartAsync();

        var negotiateResponse = NegotiateProtocol.ParseResponse(stream.ToArray());

        Assert.True(manager.TryGetConnection(negotiateResponse.ConnectionToken, out var context));

        Assert.True(context.AuthenticationExpiration > DateTimeOffset.UtcNow);
        Assert.True(context.AuthenticationExpiration < DateTimeOffset.MaxValue);

        await connection.DisposeAsync();
    }

    [Theory]
    [InlineData(HttpTransportType.LongPolling)]
    [InlineData(HttpTransportType.ServerSentEvents)]
    [InlineData(HttpTransportType.WebSockets)]
    public async Task AuthenticationExpirationUsesCorrectScheme(HttpTransportType transportType)
    {
        var SecurityKey = new SymmetricSecurityKey(SHA256.HashData(Guid.NewGuid().ToByteArray()));
        var JwtTokenHandler = new JwtSecurityTokenHandler();

        using var host = CreateHost(services =>
        {
            // Set default to Cookie auth but use JWT auth for the endpoint
            // This makes sure we take the scheme into account when grabbing the token expiration
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie()
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        LifetimeValidator = (before, expires, token, parameters) => expires > DateTime.UtcNow,
                        ValidateAudience = false,
                        ValidateIssuer = false,
                        ValidateActor = false,
                        ValidateLifetime = true,
                        IssuerSigningKey = SecurityKey
                    };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        if (!string.IsNullOrEmpty(accessToken) &&
                            (context.HttpContext.WebSockets.IsWebSocketRequest || context.Request.Headers["Accept"] == "text/event-stream"))
                        {
                            context.Token = context.Request.Query["access_token"];
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        }, endpoints =>
        {
            endpoints.MapConnectionHandler<JwtConnectionHandler>("/foo", o => o.CloseOnAuthenticationExpiration = true);

            endpoints.MapGet("/generatetoken", context =>
            {
                return context.Response.WriteAsync(GenerateToken(context));
            });

            string GenerateToken(HttpContext httpContext)
            {
                var claims = new[] { new Claim(ClaimTypes.NameIdentifier, httpContext.Request.Query["user"]) };
                var credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
                var token = new JwtSecurityToken("SignalRTestServer", "SignalRTests", claims, expires: DateTime.UtcNow.AddMinutes(1), signingCredentials: credentials);
                return JwtTokenHandler.WriteToken(token);
            }
        }, LoggerFactory);

        host.Start();

        var manager = host.Services.GetRequiredService<HttpConnectionManager>();
        var url = host.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.Single();

        string token;
        using (var client = new HttpClient())
        {
            client.BaseAddress = new Uri(url);

            var response = await client.GetAsync("generatetoken?user=bob");
            token = await response.Content.ReadAsStringAsync();
        }

        url += "/foo";
        var stream = new MemoryStream();
        var connection = new HttpConnection(
            new HttpConnectionOptions()
            {
                Url = new Uri(url),
                AccessTokenProvider = () => Task.FromResult(token),
                Transports = transportType,
                DefaultTransferFormat = TransferFormat.Text,
                HttpMessageHandlerFactory = handler => new GetNegotiateHttpHandler(handler, stream),
            },
            LoggerFactory);

        await connection.StartAsync();

        var negotiateResponse = NegotiateProtocol.ParseResponse(stream.ToArray());

        Assert.True(manager.TryGetConnection(negotiateResponse.ConnectionToken, out var context));

        Assert.True(context.AuthenticationExpiration > DateTimeOffset.UtcNow);
        Assert.True(context.AuthenticationExpiration < DateTimeOffset.MaxValue);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task AuthenticationExpirationSetToMaxValueByDefault()
    {
        using var host = CreateHost(services =>
        {
            services.AddAuthentication();
        }, endpoints =>
        {
            endpoints.MapConnectionHandler<TestConnectionHandler>("/foo");
        }, LoggerFactory);

        host.Start();

        var manager = host.Services.GetRequiredService<HttpConnectionManager>();
        var url = host.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.Single();

        url += "/foo";
        var stream = new MemoryStream();
        var connection = new HttpConnection(
            new HttpConnectionOptions()
            {
                Url = new Uri(url),
                DefaultTransferFormat = TransferFormat.Text,
                HttpMessageHandlerFactory = handler => new GetNegotiateHttpHandler(handler, stream)
            },
            LoggerFactory);

        await connection.StartAsync();

        var negotiateResponse = NegotiateProtocol.ParseResponse(stream.ToArray());

        Assert.True(manager.TryGetConnection(negotiateResponse.ConnectionToken, out var context));

        Assert.Equal(DateTimeOffset.MaxValue, context.AuthenticationExpiration);

        await connection.DisposeAsync();
    }

    [Theory]
    [InlineData(HttpTransportType.ServerSentEvents)]
    [InlineData(HttpTransportType.WebSockets)]
    public async Task RequestTimeoutDisabledWhenConnected(HttpTransportType transportType)
    {
        using (StartVerifiableLog())
        {
            using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseKestrel()
                .ConfigureLogging(o =>
                {
                    o.AddProvider(new ForwardingLoggerProvider(LoggerFactory));
                })
                .ConfigureServices(services =>
                {
                    services.AddConnections();

                    // Since tests run in parallel, it's possible multiple servers will startup,
                    // we use an ephemeral key provider and repository to avoid filesystem contention issues
                    services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();

                    services.Configure<KeyManagementOptions>(options =>
                    {
                        options.XmlRepository = new EphemeralXmlRepository();
                    });
                })
                .Configure(app =>
                {
                    app.Use((c, n) =>
                    {
                        c.Features.Set<IHttpRequestTimeoutFeature>(new HttpRequestTimeoutFeature());
                        Assert.True(((HttpRequestTimeoutFeature)c.Features.Get<IHttpRequestTimeoutFeature>()).Enabled);
                        return n(c);
                    });
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapConnectionHandler<TestConnectionHandler>("/foo");
                    });
                })
                .UseUrls("http://127.0.0.1:0");
            })
            .Build();

            host.Start();

            var manager = host.Services.GetRequiredService<HttpConnectionManager>();
            var url = host.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.Single();

            var stream = new MemoryStream();
            var connection = new HttpConnection(
                new HttpConnectionOptions()
                {
                    Url = new Uri(url + "/foo"),
                    Transports = transportType,
                    DefaultTransferFormat = TransferFormat.Text,
                    HttpMessageHandlerFactory = handler => new GetNegotiateHttpHandler(handler, stream)
                },
                LoggerFactory);

            await connection.StartAsync();

            var negotiateResponse = NegotiateProtocol.ParseResponse(stream.ToArray());

            Assert.True(manager.TryGetConnection(negotiateResponse.ConnectionToken, out var context));
            var feature = Assert.IsType<HttpRequestTimeoutFeature>(context.Features.Get<IHttpContextFeature>()?.HttpContext.Features.Get<IHttpRequestTimeoutFeature>());
            Assert.False(feature.Enabled);

            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task DisableRequestTimeoutInLongPolling()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory, TimeSpan.FromSeconds(5));
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var options = new HttpConnectionDispatcherOptions();
            var connection = manager.CreateConnection(options);
            connection.TransportType = HttpTransportType.LongPolling;

            var services = new ServiceCollection();
            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<HttpContextConnectionHandler>();
            var app = builder.Build();
            var context = MakeRequest("/foo", connection, services);
            context.Features.Set<IHttpRequestTimeoutFeature>(new HttpRequestTimeoutFeature());
            Assert.True(((HttpRequestTimeoutFeature)context.Features.Get<IHttpRequestTimeoutFeature>()).Enabled);

            // Initial poll will complete immediately
            await dispatcher.ExecuteAsync(context, options, app).DefaultTimeout();
            Assert.False(((HttpRequestTimeoutFeature)context.Features.Get<IHttpRequestTimeoutFeature>()).Enabled);

            context.Features.Set<IHttpRequestTimeoutFeature>(new HttpRequestTimeoutFeature());
            Assert.True(((HttpRequestTimeoutFeature)context.Features.Get<IHttpRequestTimeoutFeature>()).Enabled);
            var pollTask = dispatcher.ExecuteAsync(context, options, app);
            // disables on every poll
            Assert.False(((HttpRequestTimeoutFeature)context.Features.Get<IHttpRequestTimeoutFeature>()).Enabled);

            await connection.DisposeAsync().DefaultTimeout();
        }
    }

    private class GetNegotiateHttpHandler : DelegatingHandler
    {
        private readonly MemoryStream _stream;
        private bool _read;

        public GetNegotiateHttpHandler(HttpMessageHandler handler, MemoryStream stream)
            : base(handler)
        {
            _stream = stream;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (!_read)
            {
                await response.Content.CopyToAsync(_stream);
                response.Content = new ByteArrayContent(_stream.ToArray());
                _stream.Position = 0;
                _read = true;
            }
            return response;
        }
    }

    private class ForwardingLoggerProvider : ILoggerProvider
    {
        private readonly ILoggerFactory _loggerFactory;

        public ForwardingLoggerProvider(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggerFactory.CreateLogger(categoryName);
        }
    }

    private static IHost CreateHost(Action<IServiceCollection> configureServices, Action<IEndpointRouteBuilder> configureEndpoints,
        ILoggerFactory loggerFactory)
    {
        return new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseKestrel()
                .ConfigureLogging(o =>
                {
                    o.AddProvider(new ForwardingLoggerProvider(loggerFactory));
                })
                .ConfigureServices(services =>
                {
                    services.AddConnections();
                    configureServices(services);
                    services.AddAuthorization();

                    // Since tests run in parallel, it's possible multiple servers will startup,
                    // we use an ephemeral key provider and repository to avoid filesystem contention issues
                    services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();

                    services.Configure<KeyManagementOptions>(options =>
                    {
                        options.XmlRepository = new EphemeralXmlRepository();
                    });
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        configureEndpoints(endpoints);
                    });
                })
                .UseUrls("http://127.0.0.1:0");
            })
            .Build();
    }

    private static async Task CheckTransportSupported(HttpTransportType supportedTransports, HttpTransportType transportType, int status, ILoggerFactory loggerFactory)
    {
        var manager = CreateConnectionManager(loggerFactory);
        var connection = manager.CreateConnection();

        var dispatcher = CreateDispatcher(manager, loggerFactory);
        using (var strm = new MemoryStream())
        {
            var context = new DefaultHttpContext();
            context.Features.Set<IHttpResponseFeature>(new ResponseFeature());
            context.Response.Body = strm;
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddSingleton<ImmediatelyCompleteConnectionHandler>();
            SetTransport(context, transportType);
            context.Request.Path = "/foo";
            context.Request.Method = "GET";
            var values = new Dictionary<string, StringValues>();
            values["id"] = connection.ConnectionToken;
            values["negotiateVersion"] = "1";
            var qs = new QueryCollection(values);
            context.Request.Query = qs;
            context.RequestServices = services.BuildServiceProvider();

            var builder = new ConnectionBuilder(services.BuildServiceProvider());
            builder.UseConnectionHandler<ImmediatelyCompleteConnectionHandler>();
            var app = builder.Build();
            var options = new HttpConnectionDispatcherOptions();
            options.Transports = supportedTransports;

            await dispatcher.ExecuteAsync(context, options, app);
            Assert.Equal(status, context.Response.StatusCode);
            await strm.FlushAsync();

            // Check the message for 404
            if (status == 404)
            {
                Assert.Equal($"{transportType} transport not supported by this end point type", Encoding.UTF8.GetString(strm.ToArray()));
            }

            // Check cache headers for LongPolling transport
            if (transportType == HttpTransportType.LongPolling)
            {
                AssertResponseHasCacheHeaders(context.Response);
            }
        }
    }

    private static DefaultHttpContext MakeRequest(string path, HttpConnectionContext connection, IServiceCollection serviceCollection, string format = null)
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new ResponseFeature());
        context.Request.Path = path;
        context.Request.Method = "GET";
        var values = new Dictionary<string, StringValues>();
        values["id"] = connection.ConnectionToken;
        values["negotiateVersion"] = "1";
        if (format != null)
        {
            values["format"] = format;
        }
        var qs = new QueryCollection(values);
        context.Request.Query = qs;
        context.Response.Body = new MemoryStream();
        context.RequestServices = serviceCollection.BuildServiceProvider();
        return context;
    }

    private static void SetTransport(HttpContext context, HttpTransportType transportType, SyncPoint sync = null)
    {
        switch (transportType)
        {
            case HttpTransportType.WebSockets:
                context.Features.Set<IHttpWebSocketFeature>(new TestWebSocketConnectionFeature(sync));
                break;
            case HttpTransportType.ServerSentEvents:
                context.Request.Headers["Accept"] = "text/event-stream";
                break;
            default:
                break;
        }
    }

    private static HttpConnectionManager CreateConnectionManager(ILoggerFactory loggerFactory, HttpConnectionsMetrics metrics = null)
    {
        return CreateConnectionManager(loggerFactory, null, metrics);
    }

    private static HttpConnectionManager CreateConnectionManager(ILoggerFactory loggerFactory, TimeSpan? disconnectTimeout, HttpConnectionsMetrics metrics = null)
    {
        var connectionOptions = new ConnectionOptions();
        connectionOptions.DisconnectTimeout = disconnectTimeout;
        return new HttpConnectionManager(
            loggerFactory ?? new LoggerFactory(),
            new EmptyApplicationLifetime(),
            Options.Create(connectionOptions),
            metrics ?? new HttpConnectionsMetrics(new TestMeterFactory()));
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

    private static void AssertResponseHasCacheHeaders(HttpResponse response)
    {
        Assert.Equal("no-cache, no-store", response.Headers.CacheControl);
        Assert.Equal("no-cache", response.Headers.Pragma);
        Assert.Equal("Thu, 01 Jan 1970 00:00:00 GMT", response.Headers.Expires);
    }

    private static HttpConnectionDispatcher CreateDispatcher(HttpConnectionManager manager, ILoggerFactory loggerFactory, HttpConnectionsMetrics metrics = null)
    {
        return new HttpConnectionDispatcher(manager, loggerFactory, metrics ?? new HttpConnectionsMetrics(new TestMeterFactory()));
    }
}

public class NeverEndingConnectionHandler : ConnectionHandler
{
    public override Task OnConnectedAsync(ConnectionContext connection)
    {
        var tcs = new TaskCompletionSource();
        return tcs.Task;
    }
}

public class BlockingConnectionHandler : ConnectionHandler
{
    public override Task OnConnectedAsync(ConnectionContext connection)
    {
        var result = connection.Transport.Input.ReadAsync().AsTask().Result;
        connection.Transport.Input.AdvanceTo(result.Buffer.End);
        return Task.CompletedTask;
    }
}

public class SynchronusExceptionConnectionHandler : ConnectionHandler
{
    public override Task OnConnectedAsync(ConnectionContext connection)
    {
        throw new InvalidOperationException();
    }
}

public class ImmediatelyCompleteConnectionHandler : ConnectionHandler
{
    public override Task OnConnectedAsync(ConnectionContext connection)
    {
        return Task.CompletedTask;
    }
}

public class HttpContextConnectionHandler : ConnectionHandler
{
    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        while (true)
        {
            var result = await connection.Transport.Input.ReadAsync();

            try
            {
                if (result.IsCompleted)
                {
                    break;
                }

                // Make sure we have an http context
                var context = connection.GetHttpContext();
                Assert.NotNull(context);

                // Setting the response headers should have no effect
                context.Response.ContentType = "application/xml";

                // Echo the results
                await connection.Transport.Output.WriteAsync(result.Buffer.ToArray());
            }
            finally
            {
                connection.Transport.Input.AdvanceTo(result.Buffer.End);
            }
        }
    }
}

public class TestConnectionHandler : ConnectionHandler
{
    private readonly TaskCompletionSource _startedTcs = new TaskCompletionSource();

    public Task Started => _startedTcs.Task;

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        _startedTcs.TrySetResult();

        while (true)
        {
            var result = await connection.Transport.Input.ReadAsync();

            try
            {
                if (result.IsCompleted)
                {
                    break;
                }
            }
            finally
            {
                connection.Transport.Input.AdvanceTo(result.Buffer.End);
            }
        }
    }
}

public class ServiceProviderConnectionHandler : ConnectionHandler
{
    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        while (true)
        {
            var result = await connection.Transport.Input.ReadAsync();

            try
            {
                if (result.IsCompleted)
                {
                    break;
                }

                var context = connection.GetHttpContext();
                var message = context.RequestServices.GetService<MessageWrapper>();

                // Echo the results
                await connection.Transport.Output.WriteAsync(message.Buffer.ToArray());
            }
            finally
            {
                connection.Transport.Input.AdvanceTo(result.Buffer.End);
            }
        }
    }
}

[Authorize]
public class AuthConnectionHandler : ConnectionHandler
{
    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        while (true)
        {
            var result = await connection.Transport.Input.ReadAsync();

            try
            {
                if (result.IsCompleted)
                {
                    break;
                }

                // Echo the results
                await connection.Transport.Output.WriteAsync(result.Buffer.ToArray());
            }
            finally
            {
                connection.Transport.Input.AdvanceTo(result.Buffer.End);
            }
        }
    }
}

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class JwtConnectionHandler : ConnectionHandler
{
    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        while (true)
        {
            var result = await connection.Transport.Input.ReadAsync();

            try
            {
                if (result.IsCompleted)
                {
                    break;
                }

                // Echo the results
                await connection.Transport.Output.WriteAsync(result.Buffer.ToArray());
            }
            finally
            {
                connection.Transport.Input.AdvanceTo(result.Buffer.End);
            }
        }
    }
}

public class ReconnectConnectionHandler : ConnectionHandler
{
    private TaskCompletionSource<bool> _pause;

    private PipeWriter _writer;

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        _writer = connection.Transport.Output;

        connection.ConnectionClosed.Register(() =>
        {
            _pause.TrySetResult(false);
        });

#pragma warning disable CA2252 // This API requires opting into preview features
        var reconnectFeature = connection.Features.Get<IStatefulReconnectFeature>();
#pragma warning restore CA2252 // This API requires opting into preview features
        Assert.NotNull(reconnectFeature);
#pragma warning disable CA2252 // This API requires opting into preview features
        reconnectFeature.OnReconnected(NotifyReconnect);
#pragma warning restore CA2252 // This API requires opting into preview features

        do
        {
            _pause = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            while (true)
            {
                var res = await connection.Transport.Input.ReadAsync(connection.ConnectionClosed);

                try
                {
                    if (res.IsCompleted)
                    {
                        break;
                    }

                    await _writer.WriteAsync(res.Buffer.ToArray());
                }
                finally
                {
                    connection.Transport.Input.AdvanceTo(res.Buffer.End);
                }
            }
        } while (await _pause.Task);
    }

    private Task NotifyReconnect(PipeWriter writer)
    {
        _writer.Complete();
        _writer = writer;
        _pause.SetResult(true);
        return Task.CompletedTask;
    }
}

public class ResponseFeature : HttpResponseFeature
{
    public override void OnCompleted(Func<object, Task> callback, object state)
    {
    }

    public override void OnStarting(Func<object, Task> callback, object state)
    {
    }
}

public class MessageWrapper
{
    public ReadOnlySequence<byte> Buffer { get; set; }
}

internal sealed class HttpRequestTimeoutFeature : IHttpRequestTimeoutFeature
{
    public bool Enabled { get; private set; } = true;

    public CancellationToken RequestTimeoutToken => new CancellationToken();

    public void DisableTimeout()
    {
        Enabled = false;
    }
}

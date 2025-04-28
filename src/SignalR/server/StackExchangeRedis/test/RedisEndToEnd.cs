// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Tests;

// Disable running server tests in parallel so server logs can accurately be captured per test
[CollectionDefinition(Name, DisableParallelization = true)]
public class RedisEndToEndTestsCollection : ICollectionFixture<RedisServerFixture<Startup>>
{
    public const string Name = nameof(RedisEndToEndTestsCollection);
}

[Collection(RedisEndToEndTestsCollection.Name)]
public class RedisEndToEndTests : VerifiableLoggedTest
{
    private readonly RedisServerFixture<Startup> _serverFixture;

    public RedisEndToEndTests(RedisServerFixture<Startup> serverFixture)
    {
        ArgumentNullException.ThrowIfNull(serverFixture);

        _serverFixture = serverFixture;
    }

    [ConditionalTheory]
    [SkipIfDockerNotPresent]
    [MemberData(nameof(TransportTypesAndProtocolTypes))]
    public async Task HubConnectionCanSendAndReceiveMessages(HttpTransportType transportType, string protocolName)
    {
        using (StartVerifiableLog())
        {
            var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

            var connection = CreateConnection(_serverFixture.FirstServer.Url + "/echo", transportType, protocol, LoggerFactory);

            await connection.StartAsync().DefaultTimeout();
            var str = await connection.InvokeAsync<string>("Echo", "Hello, World!").DefaultTimeout();

            Assert.Equal("Hello, World!", str);

            await connection.DisposeAsync().DefaultTimeout();
        }
    }

    [ConditionalTheory]
    [SkipIfDockerNotPresent]
    [MemberData(nameof(TransportTypesAndProtocolTypes))]
    public async Task HubConnectionCanSendAndReceiveGroupMessages(HttpTransportType transportType, string protocolName)
    {
        using (StartVerifiableLog())
        {
            var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

            var connection = CreateConnection(_serverFixture.FirstServer.Url + "/echo", transportType, protocol, LoggerFactory);
            var secondConnection = CreateConnection(_serverFixture.SecondServer.Url + "/echo", transportType, protocol, LoggerFactory);

            var tcs = new TaskCompletionSource<string>();
            connection.On<string>("Echo", message => tcs.TrySetResult(message));
            var tcs2 = new TaskCompletionSource<string>();
            secondConnection.On<string>("Echo", message => tcs2.TrySetResult(message));

            var groupName = $"TestGroup_{transportType}_{protocolName}_{Guid.NewGuid()}";

            await secondConnection.StartAsync().DefaultTimeout();
            await connection.StartAsync().DefaultTimeout();
            await connection.InvokeAsync("AddSelfToGroup", groupName).DefaultTimeout();
            await secondConnection.InvokeAsync("AddSelfToGroup", groupName).DefaultTimeout();
            await connection.InvokeAsync("EchoGroup", groupName, "Hello, World!").DefaultTimeout();

            Assert.Equal("Hello, World!", await tcs.Task.DefaultTimeout());
            Assert.Equal("Hello, World!", await tcs2.Task.DefaultTimeout());

            await connection.DisposeAsync().DefaultTimeout();
        }
    }

    [ConditionalTheory]
    [SkipIfDockerNotPresent]
    [MemberData(nameof(TransportTypesAndProtocolTypes))]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/59991")]
    public async Task CanSendAndReceiveUserMessagesFromMultipleConnectionsWithSameUser(HttpTransportType transportType, string protocolName)
    {
        using (StartVerifiableLog())
        {
            var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

            var connection = CreateConnection(_serverFixture.FirstServer.Url + "/echo", transportType, protocol, LoggerFactory, userName: "userA");
            var secondConnection = CreateConnection(_serverFixture.SecondServer.Url + "/echo", transportType, protocol, LoggerFactory, userName: "userA");

            var tcs = new TaskCompletionSource<string>();
            connection.On<string>("Echo", message => tcs.TrySetResult(message));
            var tcs2 = new TaskCompletionSource<string>();
            secondConnection.On<string>("Echo", message => tcs2.TrySetResult(message));

            await secondConnection.StartAsync().DefaultTimeout();
            await connection.StartAsync().DefaultTimeout();
            await connection.InvokeAsync("EchoUser", "userA", "Hello, World!").DefaultTimeout();

            Assert.Equal("Hello, World!", await tcs.Task.DefaultTimeout());
            Assert.Equal("Hello, World!", await tcs2.Task.DefaultTimeout());

            await connection.DisposeAsync().DefaultTimeout();
            await secondConnection.DisposeAsync().DefaultTimeout();
        }
    }

    [ConditionalTheory]
    [SkipIfDockerNotPresent]
    [MemberData(nameof(TransportTypesAndProtocolTypes))]
    public async Task CanSendAndReceiveUserMessagesWhenOneConnectionWithUserDisconnects(HttpTransportType transportType, string protocolName)
    {
        // Regression test:
        // When multiple connections from the same user were connected and one left, it used to unsubscribe from the user channel
        // Now we keep track of users connections and only unsubscribe when no users are listening
        using (StartVerifiableLog())
        {
            var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

            var firstConnection = CreateConnection(_serverFixture.FirstServer.Url + "/echo", transportType, protocol, LoggerFactory, userName: "userA");
            var secondConnection = CreateConnection(_serverFixture.SecondServer.Url + "/echo", transportType, protocol, LoggerFactory, userName: "userA");

            var tcs = new TaskCompletionSource<string>();
            firstConnection.On<string>("Echo", message => tcs.TrySetResult(message));

            await secondConnection.StartAsync().DefaultTimeout();
            await firstConnection.StartAsync().DefaultTimeout();
            await secondConnection.DisposeAsync().DefaultTimeout();
            await firstConnection.InvokeAsync("EchoUser", "userA", "Hello, World!").DefaultTimeout();

            Assert.Equal("Hello, World!", await tcs.Task.DefaultTimeout());

            await firstConnection.DisposeAsync().DefaultTimeout();
        }
    }

    [ConditionalTheory]
    [SkipIfDockerNotPresent]
    [MemberData(nameof(TransportTypesAndProtocolTypes))]
    public async Task HubConnectionCanSendAndReceiveGroupMessagesGroupNameWithPatternIsTreatedAsLiteral(HttpTransportType transportType, string protocolName)
    {
        using (StartVerifiableLog())
        {
            var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

            var connection = CreateConnection(_serverFixture.FirstServer.Url + "/echo", transportType, protocol, LoggerFactory);
            var secondConnection = CreateConnection(_serverFixture.SecondServer.Url + "/echo", transportType, protocol, LoggerFactory);

            var tcs = new TaskCompletionSource<string>();
            connection.On<string>("Echo", message => tcs.TrySetResult(message));
            var tcs2 = new TaskCompletionSource<string>();
            secondConnection.On<string>("Echo", message => tcs2.TrySetResult(message));

            var groupName = $"TestGroup_{transportType}_{protocolName}_{Guid.NewGuid()}";

            await secondConnection.StartAsync().DefaultTimeout();
            await connection.StartAsync().DefaultTimeout();
            await connection.InvokeAsync("AddSelfToGroup", "*").DefaultTimeout();
            await secondConnection.InvokeAsync("AddSelfToGroup", groupName).DefaultTimeout();
            await connection.InvokeAsync("EchoGroup", groupName, "Hello, World!").DefaultTimeout();

            Assert.Equal("Hello, World!", await tcs2.Task.DefaultTimeout());
            Assert.False(tcs.Task.IsCompleted);

            await connection.InvokeAsync("EchoGroup", "*", "Hello, World!").DefaultTimeout();
            Assert.Equal("Hello, World!", await tcs.Task.DefaultTimeout());

            await connection.DisposeAsync().DefaultTimeout();
        }
    }

    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/53644")]
    [ConditionalTheory]
    [SkipIfDockerNotPresent]
    [MemberData(nameof(TransportTypesAndProtocolTypes))]
    public async Task CanSendAndReceiveUserMessagesUserNameWithPatternIsTreatedAsLiteral(HttpTransportType transportType, string protocolName)
    {
        using (StartVerifiableLog())
        {
            var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

            var connection = CreateConnection(_serverFixture.FirstServer.Url + "/echo", transportType, protocol, LoggerFactory, userName: "*");
            var secondConnection = CreateConnection(_serverFixture.SecondServer.Url + "/echo", transportType, protocol, LoggerFactory, userName: "userA");

            var tcs = new TaskCompletionSource<string>();
            connection.On<string>("Echo", message => tcs.TrySetResult(message));
            var tcs2 = new TaskCompletionSource<string>();
            secondConnection.On<string>("Echo", message => tcs2.TrySetResult(message));

            await secondConnection.StartAsync().DefaultTimeout();
            await connection.StartAsync().DefaultTimeout();
            await connection.InvokeAsync("EchoUser", "userA", "Hello, World!").DefaultTimeout();

            Assert.Equal("Hello, World!", await tcs2.Task.DefaultTimeout());
            Assert.False(tcs.Task.IsCompleted);

            await connection.InvokeAsync("EchoUser", "*", "Hello, World!").DefaultTimeout();
            Assert.Equal("Hello, World!", await tcs.Task.DefaultTimeout());

            await connection.DisposeAsync().DefaultTimeout();
            await secondConnection.DisposeAsync().DefaultTimeout();
        }
    }

    [ConditionalTheory]
    [SkipIfDockerNotPresent]
    [InlineData("messagepack")]
    [InlineData("json")]
    public async Task StatefulReconnectPreservesMessageFromOtherServer(string protocolName)
    {
        using (StartVerifiableLog())
        {
            var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

            ClientWebSocket innerWs = null;
            WebSocketWrapper ws = null;
            TaskCompletionSource reconnectTcs = null;
            TaskCompletionSource startedReconnectTcs = null;

            var connection = CreateConnection(_serverFixture.FirstServer.Url + "/stateful", HttpTransportType.WebSockets, protocol, LoggerFactory,
                customizeConnection: builder =>
                {
                    builder.WithStatefulReconnect();
                    builder.Services.Configure<HttpConnectionOptions>(o =>
                    {
                        // Replace the websocket creation for the first connection so we can make the client think there was an ungraceful closure
                        // Which will trigger the stateful reconnect flow
                        o.WebSocketFactory = async (context, token) =>
                        {
                            if (reconnectTcs is null)
                            {
                                reconnectTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                                startedReconnectTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                            }
                            else
                            {
                                startedReconnectTcs.SetResult();
                                // We only want to wait on the reconnect, not the initial connection attempt
                                await reconnectTcs.Task.DefaultTimeout();
                            }

                            innerWs = new ClientWebSocket();
                            ws = new WebSocketWrapper(innerWs);
                            await innerWs.ConnectAsync(context.Uri, token);

                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    while (innerWs.State == WebSocketState.Open)
                                    {
                                        var buffer = new byte[1024];
                                        var res = await innerWs.ReceiveAsync(buffer, default);
                                        ws.SetReceiveResult((res, buffer.AsMemory(0, res.Count)));
                                    }
                                }
                                // Log but ignore receive errors, that likely just means the connection closed
                                catch (Exception ex)
                                {
                                    Logger.LogInformation(ex, "Error while reading from inner websocket");
                                }
                            });

                            return ws;
                        };
                    });
                });
            var secondConnection = CreateConnection(_serverFixture.SecondServer.Url + "/stateful", HttpTransportType.WebSockets, protocol, LoggerFactory);

            var tcs = new TaskCompletionSource<string>();
            connection.On<string>("SendToAll", message => tcs.TrySetResult(message));

            var tcs2 = new TaskCompletionSource<string>();
            secondConnection.On<string>("SendToAll", message => tcs2.TrySetResult(message));

            await connection.StartAsync().DefaultTimeout();
            await secondConnection.StartAsync().DefaultTimeout();

            // Close first connection before the second connection sends a message to all clients
            await ws.CloseOutputAsync(WebSocketCloseStatus.InternalServerError, statusDescription: null, default);
            await startedReconnectTcs.Task.DefaultTimeout();

            // Send to all clients, since both clients are on different servers this means the backplane will be used
            // And we want to test that messages are still preserved for stateful reconnect purposes when a client disconnects
            // But is on a different server from the original message sender.
            await secondConnection.SendAsync("SendToAll", "test message").DefaultTimeout();

            // Check that second connection still receives the message
            Assert.Equal("test message", await tcs2.Task.DefaultTimeout());
            Assert.False(tcs.Task.IsCompleted);

            // allow first connection to reconnect
            reconnectTcs.SetResult();

            // Check that first connection received the message once it reconnected
            Assert.Equal("test message", await tcs.Task.DefaultTimeout());

            await connection.DisposeAsync().DefaultTimeout();
        }
    }

    private static HubConnection CreateConnection(string url, HttpTransportType transportType, IHubProtocol protocol, ILoggerFactory loggerFactory, string userName = null,
        Action<IHubConnectionBuilder> customizeConnection = null)
    {
        var hubConnectionBuilder = new HubConnectionBuilder()
            .WithLoggerFactory(loggerFactory)
            .WithUrl(url, transportType, httpConnectionOptions =>
            {
                if (!string.IsNullOrEmpty(userName))
                {
                    httpConnectionOptions.Headers["UserName"] = userName;
                }
            });

        hubConnectionBuilder.Services.AddSingleton(protocol);

        customizeConnection?.Invoke(hubConnectionBuilder);

        return hubConnectionBuilder.Build();
    }

    private static IEnumerable<HttpTransportType> TransportTypes()
    {
        if (TestHelpers.IsWebSocketsSupported())
        {
            yield return HttpTransportType.WebSockets;
        }
        yield return HttpTransportType.ServerSentEvents;
        yield return HttpTransportType.LongPolling;
    }

    public static IEnumerable<object[]> TransportTypesAndProtocolTypes
    {
        get
        {
            foreach (var transport in TransportTypes())
            {
                yield return new object[] { transport, "json" };

                if (transport != HttpTransportType.ServerSentEvents)
                {
                    yield return new object[] { transport, "messagepack" };
                }
            }
        }
    }

    internal sealed class WebSocketWrapper : WebSocket
    {
        private readonly WebSocket _inner;
        private TaskCompletionSource<(WebSocketReceiveResult, ReadOnlyMemory<byte>)> _receiveTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public WebSocketWrapper(WebSocket inner)
        {
            _inner = inner;
        }

        public override WebSocketCloseStatus? CloseStatus => _inner.CloseStatus;

        public override string CloseStatusDescription => _inner.CloseStatusDescription;

        public override WebSocketState State => _inner.State;

        public override string SubProtocol => _inner.SubProtocol;

        public override void Abort()
        {
            _inner.Abort();
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            return _inner.CloseAsync(closeStatus, statusDescription, cancellationToken);
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            _receiveTcs.TrySetException(new IOException("force reconnect"));
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _inner.Dispose();
        }

        public void SetReceiveResult((WebSocketReceiveResult, ReadOnlyMemory<byte>) result)
        {
            _receiveTcs.SetResult(result);
        }

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            var res = await _receiveTcs.Task;
            // Handle zero-byte reads
            if (buffer.Count == 0)
            {
                return res.Item1;
            }
            _receiveTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            res.Item2.CopyTo(buffer);
            return res.Item1;
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            return _inner.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
        }
    }
}

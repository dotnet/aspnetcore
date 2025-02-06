// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

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

    private static HubConnection CreateConnection(string url, HttpTransportType transportType, IHubProtocol protocol, ILoggerFactory loggerFactory, string userName = null)
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
}

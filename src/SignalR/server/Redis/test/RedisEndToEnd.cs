// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Redis.Tests
{
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

        public RedisEndToEndTests(RedisServerFixture<Startup> serverFixture, ITestOutputHelper output) : base(output)
        {
            if (serverFixture == null)
            {
                throw new ArgumentNullException(nameof(serverFixture));
            }

            _serverFixture = serverFixture;
        }

        [ConditionalTheory]
        [SkipIfDockerNotPresent]
        [MemberData(nameof(TransportTypesAndProtocolTypes))]
        public async Task HubConnectionCanSendAndReceiveMessages(HttpTransportType transportType, string protocolName)
        {
            using (StartVerifiableLog(out var loggerFactory, testName:
                $"{nameof(HubConnectionCanSendAndReceiveMessages)}_{transportType.ToString()}_{protocolName}"))
            {
                var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

                var connection = CreateConnection(_serverFixture.FirstServer.Url + "/echo", transportType, protocol, loggerFactory);

                await connection.StartAsync().OrTimeout();
                var str = await connection.InvokeAsync<string>("Echo", "Hello, World!").OrTimeout();

                Assert.Equal("Hello, World!", str);

                await connection.DisposeAsync().OrTimeout();
            }
        }

        [ConditionalTheory]
        [SkipIfDockerNotPresent]
        [MemberData(nameof(TransportTypesAndProtocolTypes))]
        public async Task HubConnectionCanSendAndReceiveGroupMessages(HttpTransportType transportType, string protocolName)
        {
            using (StartVerifiableLog(out var loggerFactory, testName:
                $"{nameof(HubConnectionCanSendAndReceiveGroupMessages)}_{transportType.ToString()}_{protocolName}"))
            {
                var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

                var connection = CreateConnection(_serverFixture.FirstServer.Url + "/echo", transportType, protocol, loggerFactory);
                var secondConnection = CreateConnection(_serverFixture.SecondServer.Url + "/echo", transportType, protocol, loggerFactory);

                var tcs = new TaskCompletionSource<string>();
                connection.On<string>("Echo", message => tcs.TrySetResult(message));
                var tcs2 = new TaskCompletionSource<string>();
                secondConnection.On<string>("Echo", message => tcs2.TrySetResult(message));

                await secondConnection.StartAsync().OrTimeout();
                await connection.StartAsync().OrTimeout();
                await connection.InvokeAsync("AddSelfToGroup", "Test").OrTimeout();
                await secondConnection.InvokeAsync("AddSelfToGroup", "Test").OrTimeout();
                await connection.InvokeAsync("EchoGroup", "Test", "Hello, World!").OrTimeout();

                Assert.Equal("Hello, World!", await tcs.Task.OrTimeout());
                Assert.Equal("Hello, World!", await tcs2.Task.OrTimeout());

                await connection.DisposeAsync().OrTimeout();
            }
        }

        [ConditionalTheory]
        [SkipIfDockerNotPresent]
        [MemberData(nameof(TransportTypesAndProtocolTypes))]
        public async Task CanSendAndReceiveUserMessagesFromMultipleConnectionsWithSameUser(HttpTransportType transportType, string protocolName)
        {
            using (StartVerifiableLog(out var loggerFactory, testName:
                $"{nameof(CanSendAndReceiveUserMessagesFromMultipleConnectionsWithSameUser)}_{transportType.ToString()}_{protocolName}"))
            {
                var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

                var connection = CreateConnection(_serverFixture.FirstServer.Url + "/echo", transportType, protocol, loggerFactory, userName: "userA");
                var secondConnection = CreateConnection(_serverFixture.SecondServer.Url + "/echo", transportType, protocol, loggerFactory, userName: "userA");

                var tcs = new TaskCompletionSource<string>();
                connection.On<string>("Echo", message => tcs.TrySetResult(message));
                var tcs2 = new TaskCompletionSource<string>();
                secondConnection.On<string>("Echo", message => tcs2.TrySetResult(message));

                await secondConnection.StartAsync().OrTimeout();
                await connection.StartAsync().OrTimeout();
                await connection.InvokeAsync("EchoUser", "userA", "Hello, World!").OrTimeout();

                Assert.Equal("Hello, World!", await tcs.Task.OrTimeout());
                Assert.Equal("Hello, World!", await tcs2.Task.OrTimeout());

                await connection.DisposeAsync().OrTimeout();
                await secondConnection.DisposeAsync().OrTimeout();
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
            using (StartVerifiableLog(out var loggerFactory, testName:
                $"{nameof(CanSendAndReceiveUserMessagesWhenOneConnectionWithUserDisconnects)}_{transportType.ToString()}_{protocolName}"))
            {
                var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

                var firstConnection = CreateConnection(_serverFixture.FirstServer.Url + "/echo", transportType, protocol, loggerFactory, userName: "userA");
                var secondConnection = CreateConnection(_serverFixture.SecondServer.Url + "/echo", transportType, protocol, loggerFactory, userName: "userA");

                var tcs = new TaskCompletionSource<string>();
                firstConnection.On<string>("Echo", message => tcs.TrySetResult(message));

                await secondConnection.StartAsync().OrTimeout();
                await firstConnection.StartAsync().OrTimeout();
                await secondConnection.DisposeAsync().OrTimeout();
                await firstConnection.InvokeAsync("EchoUser", "userA", "Hello, World!").OrTimeout();

                Assert.Equal("Hello, World!", await tcs.Task.OrTimeout());

                await firstConnection.DisposeAsync().OrTimeout();
            }
        }

        [ConditionalTheory]
        [SkipIfDockerNotPresent]
        [MemberData(nameof(TransportTypesAndProtocolTypes))]
        public async Task HubConnectionCanSendAndReceiveGroupMessagesGroupNameWithPatternIsTreatedAsLiteral(HttpTransportType transportType, string protocolName)
        {
            using (StartVerifiableLog(out var loggerFactory, testName:
                $"{nameof(HubConnectionCanSendAndReceiveGroupMessagesGroupNameWithPatternIsTreatedAsLiteral)}_{transportType.ToString()}_{protocolName}"))
            {
                var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

                var connection = CreateConnection(_serverFixture.FirstServer.Url + "/echo", transportType, protocol, loggerFactory);
                var secondConnection = CreateConnection(_serverFixture.SecondServer.Url + "/echo", transportType, protocol, loggerFactory);

                var tcs = new TaskCompletionSource<string>();
                connection.On<string>("Echo", message => tcs.TrySetResult(message));
                var tcs2 = new TaskCompletionSource<string>();
                secondConnection.On<string>("Echo", message => tcs2.TrySetResult(message));

                var groupName = $"TestGroup_{transportType}_{protocolName}_{Guid.NewGuid()}";

                await secondConnection.StartAsync().OrTimeout();
                await connection.StartAsync().OrTimeout();
                await connection.InvokeAsync("AddSelfToGroup", "*").OrTimeout();
                await secondConnection.InvokeAsync("AddSelfToGroup", groupName).OrTimeout();
                await connection.InvokeAsync("EchoGroup", groupName, "Hello, World!").OrTimeout();

                Assert.Equal("Hello, World!", await tcs2.Task.OrTimeout());
                Assert.False(tcs.Task.IsCompleted);

                await connection.InvokeAsync("EchoGroup", "*", "Hello, World!").OrTimeout();
                Assert.Equal("Hello, World!", await tcs.Task.OrTimeout());

                await connection.DisposeAsync().OrTimeout();
            }
        }

        [ConditionalTheory]
        [SkipIfDockerNotPresent]
        [MemberData(nameof(TransportTypesAndProtocolTypes))]
        public async Task CanSendAndReceiveUserMessagesUserNameWithPatternIsTreatedAsLiteral(HttpTransportType transportType, string protocolName)
        {
            using (StartVerifiableLog(out var loggerFactory, testName:
                $"{nameof(CanSendAndReceiveUserMessagesUserNameWithPatternIsTreatedAsLiteral)}_{transportType.ToString()}_{protocolName}"))
            {
                var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

                var connection = CreateConnection(_serverFixture.FirstServer.Url + "/echo", transportType, protocol, loggerFactory, userName: "*");
                var secondConnection = CreateConnection(_serverFixture.SecondServer.Url + "/echo", transportType, protocol, loggerFactory, userName: "userA");

                var tcs = new TaskCompletionSource<string>();
                connection.On<string>("Echo", message => tcs.TrySetResult(message));
                var tcs2 = new TaskCompletionSource<string>();
                secondConnection.On<string>("Echo", message => tcs2.TrySetResult(message));

                await secondConnection.StartAsync().OrTimeout();
                await connection.StartAsync().OrTimeout();
                await connection.InvokeAsync("EchoUser", "userA", "Hello, World!").OrTimeout();

                Assert.Equal("Hello, World!", await tcs2.Task.OrTimeout());
                Assert.False(tcs.Task.IsCompleted);

                await connection.InvokeAsync("EchoUser", "*", "Hello, World!").OrTimeout();
                Assert.Equal("Hello, World!", await tcs.Task.OrTimeout());

                await connection.DisposeAsync().OrTimeout();
                await secondConnection.DisposeAsync().OrTimeout();
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
}

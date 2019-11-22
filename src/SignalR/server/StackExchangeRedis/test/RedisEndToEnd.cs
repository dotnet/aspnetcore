// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Tests
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

        public RedisEndToEndTests(RedisServerFixture<Startup> serverFixture)
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
            using (StartVerifiableLog())
            {
                var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

                var connection = CreateConnection(_serverFixture.FirstServer.Url + "/echo", transportType, protocol, LoggerFactory);

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

                await secondConnection.StartAsync().OrTimeout();
                await connection.StartAsync().OrTimeout();
                await connection.InvokeAsync("AddSelfToGroup", groupName).OrTimeout();
                await secondConnection.InvokeAsync("AddSelfToGroup", groupName).OrTimeout();
                await connection.InvokeAsync("EchoGroup", groupName, "Hello, World!").OrTimeout();

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
            using (StartVerifiableLog())
            {
                var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

                var connection = CreateConnection(_serverFixture.FirstServer.Url + "/echo", transportType, protocol, LoggerFactory, userName: "userA");
                var secondConnection = CreateConnection(_serverFixture.SecondServer.Url + "/echo", transportType, protocol, LoggerFactory, userName: "userA");

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
            using (StartVerifiableLog())
            {
                var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

                var firstConnection = CreateConnection(_serverFixture.FirstServer.Url + "/echo", transportType, protocol, LoggerFactory, userName: "userA");
                var secondConnection = CreateConnection(_serverFixture.SecondServer.Url + "/echo", transportType, protocol, LoggerFactory, userName: "userA");

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

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Redis.Tests
{
    [CollectionDefinition(Name)]
    public class EndToEndTestsCollection : ICollectionFixture<RedisServerFixture<Startup>>
    {
        public const string Name = "RedisEndToEndTests";
    }

    [Collection(EndToEndTestsCollection.Name)]
    public class RedisEndToEndTests : LoggedTest
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

        [ConditionalTheory(Skip = "Docker tests are flaky")]
        [SkipIfDockerNotPresent]
        [MemberData(nameof(TransportTypesAndProtocolTypes))]
        public async Task HubConnectionCanSendAndReceiveMessages(TransportType transportType, IHubProtocol protocol)
        {
            using (StartLog(out var loggerFactory, testName:
                $"{nameof(HubConnectionCanSendAndReceiveMessages)}_{transportType.ToString()}_{protocol.Name}"))
            {
                var connection = CreateConnection(_serverFixture.FirstServer.Url + "/echo", transportType, protocol, loggerFactory);

                await connection.StartAsync().OrTimeout();
                var str = await connection.InvokeAsync<string>("Echo", "Hello, World!").OrTimeout();

                Assert.Equal("Hello, World!", str);

                await connection.DisposeAsync().OrTimeout();
            }
        }

        [ConditionalTheory(Skip = "Docker tests are flaky")]
        [SkipIfDockerNotPresent]
        [MemberData(nameof(TransportTypesAndProtocolTypes))]
        public async Task HubConnectionCanSendAndReceiveGroupMessages(TransportType transportType, IHubProtocol protocol)
        {
            using (StartLog(out var loggerFactory, testName:
                $"{nameof(HubConnectionCanSendAndReceiveGroupMessages)}_{transportType.ToString()}_{protocol.Name}"))
            {
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

        private static HubConnection CreateConnection(string url, TransportType transportType, IHubProtocol protocol, ILoggerFactory loggerFactory)
        {
            return new HubConnectionBuilder()
                .WithUrl(url)
                .WithTransport(transportType)
                .WithHubProtocol(protocol)
                .WithLoggerFactory(loggerFactory)
                .Build();
        }

        private static IEnumerable<TransportType> TransportTypes()
        {
            if (TestHelpers.IsWebSocketsSupported())
            {
                yield return TransportType.WebSockets;
            }
            yield return TransportType.ServerSentEvents;
            yield return TransportType.LongPolling;
        }

        public static IEnumerable<object[]> TransportTypesAndProtocolTypes
        {
            get
            {
                foreach (var transport in TransportTypes())
                {
                    yield return new object[] { transport, new JsonHubProtocol() };
                    yield return new object[] { transport, new MessagePackHubProtocol() };
                }
            }
        }
    }
}

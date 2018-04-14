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

        [ConditionalTheory()]
        [SkipIfDockerNotPresent]
        [MemberData(nameof(TransportTypesAndProtocolTypes))]
        public async Task HubConnectionCanSendAndReceiveMessages(HttpTransportType transportType, string protocolName)
        {
            using (StartVerifableLog(out var loggerFactory, testName:
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

        [ConditionalTheory()]
        [SkipIfDockerNotPresent]
        [MemberData(nameof(TransportTypesAndProtocolTypes))]
        public async Task HubConnectionCanSendAndReceiveGroupMessages(HttpTransportType transportType, string protocolName)
        {
            using (StartVerifableLog(out var loggerFactory, testName:
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

        private static HubConnection CreateConnection(string url, HttpTransportType transportType, IHubProtocol protocol, ILoggerFactory loggerFactory)
        {
            var hubConnectionBuilder = new HubConnectionBuilder()
                .WithLoggerFactory(loggerFactory)
                .WithUrl(url, transportType);

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

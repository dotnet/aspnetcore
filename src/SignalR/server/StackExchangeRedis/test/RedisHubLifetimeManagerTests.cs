// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Specification.Tests;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Tests;

public class RedisHubLifetimeManagerTests : ScaleoutHubLifetimeManagerTests<TestRedisServer>
{
    private TestRedisServer _server;

    public class TestObject
    {
        public string TestProperty { get; set; }
    }

    private RedisHubLifetimeManager<Hub> CreateLifetimeManager(TestRedisServer server, MessagePackHubProtocolOptions messagePackOptions = null, NewtonsoftJsonHubProtocolOptions jsonOptions = null)
    {
        var options = new RedisOptions() { ConnectionFactory = async (t) => await Task.FromResult(new TestConnectionMultiplexer(server)) };
        messagePackOptions = messagePackOptions ?? new MessagePackHubProtocolOptions();
        jsonOptions = jsonOptions ?? new NewtonsoftJsonHubProtocolOptions();
        return new RedisHubLifetimeManager<Hub>(
            NullLogger<RedisHubLifetimeManager<Hub>>.Instance,
            Options.Create(options),
            new DefaultHubProtocolResolver(new IHubProtocol[]
            {
                    new NewtonsoftJsonHubProtocol(Options.Create(jsonOptions)),
                    new MessagePackHubProtocol(Options.Create(messagePackOptions)),
            }, NullLogger<DefaultHubProtocolResolver>.Instance));
    }

    [Fact]
    public async Task CamelCasedJsonIsPreservedAcrossRedisBoundary()
    {
        var server = new TestRedisServer();

        var messagePackOptions = new MessagePackHubProtocolOptions();

        var jsonOptions = new NewtonsoftJsonHubProtocolOptions();
        jsonOptions.PayloadSerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

        using (var client1 = new TestClient())
        using (var client2 = new TestClient())
        {
            // The sending manager has serializer settings
            var manager1 = CreateLifetimeManager(server, messagePackOptions, jsonOptions);

            // The receiving one doesn't matter because of how we serialize!
            var manager2 = CreateLifetimeManager(server);

            var connection1 = HubConnectionContextUtils.Create(client1.Connection);
            var connection2 = HubConnectionContextUtils.Create(client2.Connection);

            await manager1.OnConnectedAsync(connection1).DefaultTimeout();
            await manager2.OnConnectedAsync(connection2).DefaultTimeout();

            await manager1.SendAllAsync("Hello", new object[] { new TestObject { TestProperty = "Foo" } });

            var message = Assert.IsType<InvocationMessage>(await client2.ReadAsync().DefaultTimeout());
            Assert.Equal("Hello", message.Target);
            Assert.Collection(
                message.Arguments,
                arg0 =>
                {
                    var dict = Assert.IsType<JObject>(arg0);
                    Assert.Collection(dict.Properties(),
                        prop =>
                        {
                            Assert.Equal("testProperty", prop.Name);
                            Assert.Equal("Foo", prop.Value.Value<string>());
                        });
                });
        }
    }

    [Fact]
    public async Task ErrorFromConnectionFactoryLogsAndAllowsDisconnect()
    {
        var server = new TestRedisServer();

        var testSink = new TestSink();
        var logger = new TestLogger("", testSink, true);
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory
            .Setup(m => m.CreateLogger(It.IsAny<string>()))
            .Returns((string categoryName) => (ILogger)logger);
        var loggerT = mockLoggerFactory.Object.CreateLogger<RedisHubLifetimeManager<Hub>>();

        var manager = new RedisHubLifetimeManager<Hub>(
            loggerT,
            Options.Create(new RedisOptions()
            {
                ConnectionFactory = _ => throw new ApplicationException("throw from connect")
            }),
            new DefaultHubProtocolResolver(new IHubProtocol[]
            {
            }, NullLogger<DefaultHubProtocolResolver>.Instance));

        using (var client = new TestClient())
        {
            var connection = HubConnectionContextUtils.Create(client.Connection);

            var ex = await Assert.ThrowsAsync<ApplicationException>(() => manager.OnConnectedAsync(connection)).DefaultTimeout();
            Assert.Equal("throw from connect", ex.Message);

            await manager.OnDisconnectedAsync(connection).DefaultTimeout();
        }

        var logs = testSink.Writes.ToArray();
        Assert.Single(logs);
        Assert.Equal("Error connecting to Redis.", logs[0].Message);
        Assert.Equal("throw from connect", logs[0].Exception.Message);
    }

    // Smoke test that Debug.Asserts in TestSubscriber aren't hit
    [Fact]
    public async Task PatternGroupAndUser()
    {
        var server = new TestRedisServer();
        using (var client = new TestClient())
        {
            var manager = CreateLifetimeManager(server);

            var connection = HubConnectionContextUtils.Create(client.Connection);
            connection.UserIdentifier = "*";

            await manager.OnConnectedAsync(connection).DefaultTimeout();

            var groupName = "*";

            await manager.AddToGroupAsync(connection.ConnectionId, groupName).DefaultTimeout();
        }
    }

    public override TestRedisServer CreateBackplane()
    {
        return new TestRedisServer();
    }

    public override HubLifetimeManager<Hub> CreateNewHubLifetimeManager()
    {
        _server = new TestRedisServer();
        return CreateLifetimeManager(_server);
    }

    public override HubLifetimeManager<Hub> CreateNewHubLifetimeManager(TestRedisServer backplane)
    {
        return CreateLifetimeManager(backplane);
    }
}

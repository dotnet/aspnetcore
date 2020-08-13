// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR.Specification.Tests;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Tests
{
    // Add ScaleoutHubLifetimeManagerTests<TestRedisServer> back after https://github.com/aspnet/SignalR/issues/3088
    public class RedisHubLifetimeManagerTests
    {
        public class TestObject
        {
            public string TestProperty { get; set; }
        }

        private RedisHubLifetimeManager<MyHub> CreateLifetimeManager(TestRedisServer server, MessagePackHubProtocolOptions messagePackOptions = null, NewtonsoftJsonHubProtocolOptions jsonOptions = null)
        {
            var options = new RedisOptions() { ConnectionFactory = async (t) => await Task.FromResult(new TestConnectionMultiplexer(server)) };
            messagePackOptions = messagePackOptions ?? new MessagePackHubProtocolOptions();
            jsonOptions = jsonOptions ?? new NewtonsoftJsonHubProtocolOptions();
            return new RedisHubLifetimeManager<MyHub>(
                NullLogger<RedisHubLifetimeManager<MyHub>>.Instance,
                Options.Create(options),
                new DefaultHubProtocolResolver(new IHubProtocol[]
                {
                    new NewtonsoftJsonHubProtocol(Options.Create(jsonOptions)),
                    new MessagePackHubProtocol(Options.Create(messagePackOptions)),
                }, NullLogger<DefaultHubProtocolResolver>.Instance));
        }

        [Fact(Skip = "https://github.com/aspnet/SignalR/issues/3088")]
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

                await manager1.OnConnectedAsync(connection1).OrTimeout();
                await manager2.OnConnectedAsync(connection2).OrTimeout();

                await manager1.SendAllAsync("Hello", new object[] { new TestObject { TestProperty = "Foo" } });

                var message = Assert.IsType<InvocationMessage>(await client2.ReadAsync().OrTimeout());
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
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using MsgPack.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class HubConnectionBuilderTests
    {
        [Fact]
        public void HubConnectionBuiderThrowsIfConnectionFactoryNotConfigured()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => new HubConnectionBuilder().Build());
            Assert.Equal("Cannot create IConnection instance. The connection factory was not configured.", ex.Message);
        }

        [Fact]
        public void WithUrlThrowsForNullUrls()
        {
            Assert.Equal("url",
                Assert.Throws<ArgumentNullException>(() => new HubConnectionBuilder().WithUrl((string)null)).ParamName);
            Assert.Equal("url",
                Assert.Throws<ArgumentNullException>(() => new HubConnectionBuilder().WithUrl((Uri)null)).ParamName);
        }

        [Fact]
        public void WithLoggerFactoryThrowsForNullLoggerFactory()
        {
            Assert.Equal("loggerFactory",
                Assert.Throws<ArgumentNullException>(() => new HubConnectionBuilder().WithLoggerFactory(null)).ParamName);
        }

        [Fact]
        public void WithJsonHubProtocolSetsHubProtocolToJsonWithDefaultOptions()
        {
            Assert.True(new HubConnectionBuilder().WithJsonProtocol().TryGetSetting<IHubProtocol>(HubConnectionBuilderDefaults.HubProtocolKey, out var hubProtocol));
            var actualProtocol = Assert.IsType<JsonHubProtocol>(hubProtocol);
            Assert.IsType<CamelCasePropertyNamesContractResolver>(actualProtocol.PayloadSerializer.ContractResolver);
        }

        [Fact]
        public void WithJsonHubProtocolSetsHubProtocolToJsonWithProvidedOptions()
        {
            var expectedOptions = new JsonHubProtocolOptions()
            {
                PayloadSerializerSettings = new JsonSerializerSettings()
                {
                    DateFormatString = "JUST A TEST"
                }
            };

            Assert.True(new HubConnectionBuilder().WithJsonProtocol(expectedOptions).TryGetSetting<IHubProtocol>(HubConnectionBuilderDefaults.HubProtocolKey, out var hubProtocol));
            var actualProtocol = Assert.IsType<JsonHubProtocol>(hubProtocol);
            Assert.Equal("JUST A TEST", actualProtocol.PayloadSerializer.DateFormatString);
        }

        [Fact]
        public void WithMessagePackHubProtocolSetsHubProtocolToMsgPackWithDefaultOptions()
        {
            Assert.True(new HubConnectionBuilder().WithMessagePackProtocol().TryGetSetting<IHubProtocol>(HubConnectionBuilderDefaults.HubProtocolKey, out var hubProtocol));
            var actualProtocol = Assert.IsType<MessagePackHubProtocol>(hubProtocol);
            Assert.Equal(SerializationMethod.Map, actualProtocol.SerializationContext.SerializationMethod);
        }

        [Fact]
        public void WithMessagePackHubProtocolSetsHubProtocolToMsgPackWithProvidedOptions()
        {
            var expectedOptions = new MessagePackHubProtocolOptions()
            {
                SerializationContext = new SerializationContext()
                {
                    SerializationMethod = SerializationMethod.Array
                }
            };

            Assert.True(new HubConnectionBuilder().WithMessagePackProtocol(expectedOptions).TryGetSetting<IHubProtocol>(HubConnectionBuilderDefaults.HubProtocolKey, out var hubProtocol));
            var actualProtocol = Assert.IsType<MessagePackHubProtocol>(hubProtocol);
            Assert.Equal(SerializationMethod.Array, actualProtocol.SerializationContext.SerializationMethod);
        }
    }
}

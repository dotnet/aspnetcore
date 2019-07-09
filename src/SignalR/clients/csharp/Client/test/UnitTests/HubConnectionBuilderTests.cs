// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
            Assert.Equal("Cannot create HubConnection instance. An IConnectionFactory was not configured.", ex.Message);
        }

        [Fact]
        public void CannotCreateConnectionWithNoEndPoint()
        {
            var builder = new HubConnectionBuilder();
            builder.Services.AddSingleton<IConnectionFactory>(new HttpConnectionFactory(Options.Create(new HttpConnectionOptions()), NullLoggerFactory.Instance));

            var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.Equal("Cannot create HubConnection instance. An EndPoint was not configured.", ex.Message);
        }

        [Fact]
        public void AddJsonProtocolSetsHubProtocolToJsonWithDefaultOptions()
        {
            var serviceProvider = new HubConnectionBuilder().AddNewtonsoftJsonProtocol().Services.BuildServiceProvider();

            var actualProtocol = Assert.IsType<NewtonsoftJsonHubProtocol>(serviceProvider.GetService<IHubProtocol>());
            Assert.IsType<CamelCasePropertyNamesContractResolver>(actualProtocol.PayloadSerializer.ContractResolver);
        }

        [Fact]
        public void AddJsonProtocolSetsHubProtocolToJsonWithProvidedOptions()
        {
            var serviceProvider = new HubConnectionBuilder().AddNewtonsoftJsonProtocol(options =>
            {
                options.PayloadSerializerSettings = new JsonSerializerSettings
                {
                    DateFormatString = "JUST A TEST"
                };
            }).Services.BuildServiceProvider();

            var actualProtocol = Assert.IsType<NewtonsoftJsonHubProtocol>(serviceProvider.GetService<IHubProtocol>());
            Assert.Equal("JUST A TEST", actualProtocol.PayloadSerializer.DateFormatString);
        }

        [Fact]
        public void BuildCanOnlyBeCalledOnce()
        {
            var builder = new HubConnectionBuilder();
            builder.Services.AddSingleton<IConnectionFactory>(new HttpConnectionFactory(Options.Create(new HttpConnectionOptions()), NullLoggerFactory.Instance));
            builder.WithUrl("http://example.com");

            Assert.NotNull(builder.Build());

            var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.Equal("HubConnectionBuilder allows creation only of a single instance of HubConnection.", ex.Message);
        }

        [Fact]
        public void AddMessagePackProtocolSetsHubProtocolToMsgPack()
        {
            var serviceProvider = new HubConnectionBuilder().AddMessagePackProtocol().Services.BuildServiceProvider();

            Assert.IsType<MessagePackHubProtocol>(serviceProvider.GetService<IHubProtocol>());
        }
    }
}

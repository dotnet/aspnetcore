// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public class MessagePumpTests
    {
        [ConditionalTheory]
        [InlineData("http://localhost:11001/")]
        [InlineData("invalid address")]
        [InlineData("")]
        [InlineData(null)]
        public void OverridingIServerAdressesFeatureWithDirectConfiguration_WarnsOnStart(string serverAddress)
        {
            var overrideAddress = "http://localhost:11002/";

            using (var server = new MessagePump(Options.Create(new HttpSysOptions()), new LoggerFactory()))
            {
                var serverAddressesFeature = server.Features.Get<IServerAddressesFeature>();
                serverAddressesFeature.Addresses.Add(serverAddress);
                server.Listener.Options.UrlPrefixes.Add(overrideAddress);

                server.Start(new DummyApplication());

                Assert.Equal(overrideAddress, serverAddressesFeature.Addresses.Single());
            }
        }

        [ConditionalFact]
        public void UseIServerAdressesFeature_WhenNoDirectConfiguration()
        {
            var serverAddress = "http://localhost:11001/";

            using (var server = new MessagePump(Options.Create(new HttpSysOptions()), new LoggerFactory()))
            {
                var serverAddressesFeature = server.Features.Get<IServerAddressesFeature>();
                serverAddressesFeature.Addresses.Add(serverAddress);

                server.Start(new DummyApplication());
            }
        }

        [ConditionalFact]
        public void UseDefaultAddress_WhenNoServerAddressAndNoDirectConfiguration()
        {
            using (var server = new MessagePump(Options.Create(new HttpSysOptions()), new LoggerFactory()))
            {
                server.Start(new DummyApplication());

                Assert.Equal(Constants.DefaultServerAddress, server.Features.Get<IServerAddressesFeature>().Addresses.Single());
            }
        }
    }
}

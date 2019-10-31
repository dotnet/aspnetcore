// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public class MessagePumpTests
    {
        [ConditionalFact]
        public void OverridingDirectConfigurationWithIServerAddressesFeatureSucceeds()
        {
            var serverAddress = "http://localhost:11001/";
            var overrideAddress = "http://localhost:11002/";

            using (var server = Utilities.CreatePump())
            {
                var serverAddressesFeature = server.Features.Get<IServerAddressesFeature>();
                serverAddressesFeature.Addresses.Add(overrideAddress);
                serverAddressesFeature.PreferHostingUrls = true;
                server.Listener.Options.UrlPrefixes.Add(serverAddress);

                server.StartAsync(new DummyApplication(), CancellationToken.None).Wait();

                Assert.Equal(overrideAddress, serverAddressesFeature.Addresses.Single());
            }
        }

        [ConditionalTheory]
        [InlineData("http://localhost:11001/")]
        [InlineData("invalid address")]
        [InlineData("")]
        [InlineData(null)]
        public void DoesNotOverrideDirectConfigurationWithIServerAddressesFeature_IfPreferHostinUrlsFalse(string overrideAddress)
        {
            var serverAddress = "http://localhost:11002/";

            using (var server = Utilities.CreatePump())
            {
                var serverAddressesFeature = server.Features.Get<IServerAddressesFeature>();
                serverAddressesFeature.Addresses.Add(overrideAddress);
                server.Listener.Options.UrlPrefixes.Add(serverAddress);

                server.StartAsync(new DummyApplication(), CancellationToken.None).Wait();

                Assert.Equal(serverAddress, serverAddressesFeature.Addresses.Single());
            }
        }

        [ConditionalFact]
        public void DoesNotOverrideDirectConfigurationWithIServerAddressesFeature_IfAddressesIsEmpty()
        {
            var serverAddress = "http://localhost:11002/";

            using (var server = Utilities.CreatePump())
            {
                var serverAddressesFeature = server.Features.Get<IServerAddressesFeature>();
                serverAddressesFeature.PreferHostingUrls = true;
                server.Listener.Options.UrlPrefixes.Add(serverAddress);

                server.StartAsync(new DummyApplication(), CancellationToken.None).Wait();

                Assert.Equal(serverAddress, serverAddressesFeature.Addresses.Single());
            }
        }

        [ConditionalTheory]
        [InlineData("http://localhost:11001/")]
        [InlineData("invalid address")]
        [InlineData("")]
        [InlineData(null)]
        public void OverridingIServerAddressesFeatureWithDirectConfiguration_WarnsOnStart(string serverAddress)
        {
            var overrideAddress = "http://localhost:11002/";

            using (var server = Utilities.CreatePump())
            {
                var serverAddressesFeature = server.Features.Get<IServerAddressesFeature>();
                serverAddressesFeature.Addresses.Add(serverAddress);
                server.Listener.Options.UrlPrefixes.Add(overrideAddress);

                server.StartAsync(new DummyApplication(), CancellationToken.None).Wait();

                Assert.Equal(overrideAddress, serverAddressesFeature.Addresses.Single());
            }
        }

        [ConditionalFact]
        public void UseIServerAddressesFeature_WhenNoDirectConfiguration()
        {
            var serverAddress = "http://localhost:11001/";

            using (var server = Utilities.CreatePump())
            {
                var serverAddressesFeature = server.Features.Get<IServerAddressesFeature>();
                serverAddressesFeature.Addresses.Add(serverAddress);

                server.StartAsync(new DummyApplication(), CancellationToken.None).Wait();
            }
        }

        [ConditionalFact]
        public void UseDefaultAddress_WhenNoServerAddressAndNoDirectConfiguration()
        {
            using (var server = Utilities.CreatePump())
            {
                server.StartAsync(new DummyApplication(), CancellationToken.None).Wait();

                // Trailing slash is added when put in UrlPrefix.
                Assert.StartsWith(Constants.DefaultServerAddress, server.Features.Get<IServerAddressesFeature>().Addresses.Single());
            }
        }

    }
}

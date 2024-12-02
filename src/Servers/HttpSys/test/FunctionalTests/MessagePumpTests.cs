// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys;

public class MessagePumpTests : LoggedTest
{
    [ConditionalFact]
    public void OverridingDirectConfigurationWithIServerAddressesFeatureSucceeds()
    {
        var serverAddress = "http://localhost:11001/";
        var overrideAddress = "http://localhost:11002/";

        using (var server = Utilities.CreatePump(LoggerFactory))
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

        using (var server = Utilities.CreatePump(LoggerFactory))
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

        using (var server = Utilities.CreatePump(LoggerFactory))
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

        using (var server = Utilities.CreatePump(LoggerFactory))
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

        using (var server = Utilities.CreatePump(LoggerFactory))
        {
            var serverAddressesFeature = server.Features.Get<IServerAddressesFeature>();
            serverAddressesFeature.Addresses.Add(serverAddress);

            server.StartAsync(new DummyApplication(), CancellationToken.None).Wait();
        }
    }

    [ConditionalFact]
    // test is permanently quarantined due to inherent flakiness with port binding
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/28993")]
    public void UseDefaultAddress_WhenNoServerAddressAndNoDirectConfiguration()
    {
        using (var server = Utilities.CreatePump(LoggerFactory))
        {
            server.StartAsync(new DummyApplication(), CancellationToken.None).Wait();

            // Trailing slash is added when put in UrlPrefix.
            Assert.StartsWith(Constants.DefaultServerAddress, server.Features.Get<IServerAddressesFeature>().Addresses.Single());
        }
    }

}

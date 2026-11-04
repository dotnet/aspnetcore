// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.BlazorPack;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public class ComponentServiceCollectionExtensionsTest
{
    [Fact]
    public void AddServerSideSignalR_RegistersBlazorPack()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddServerSideBlazor();

        // Act
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<HubOptions<ComponentHub>>>();

        // Assert
        var protocol = Assert.Single(options.Value.SupportedProtocols);
        Assert.Equal(BlazorPackHubProtocol.ProtocolName, protocol);
    }

    [Fact]
    public void AddServerSideSignalR_RespectsGlobalHubOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddServerSideBlazor();

        services.Configure<HubOptions>(options =>
        {
            options.SupportedProtocols.Add("test");
            options.HandshakeTimeout = TimeSpan.FromMinutes(10);
        });

        // Act
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<HubOptions<ComponentHub>>>();

        // Assert
        var protocol = Assert.Single(options.Value.SupportedProtocols);
        Assert.Equal(BlazorPackHubProtocol.ProtocolName, protocol);
        Assert.Equal(TimeSpan.FromMinutes(10), options.Value.HandshakeTimeout);
    }

    [Fact]
    public void AddServerSideSignalR_ConfiguresGlobalOptionsBeforePerHubOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddServerSideBlazor().AddHubOptions(options =>
        {
            Assert.Equal(TimeSpan.FromMinutes(10), options.HandshakeTimeout);
            options.HandshakeTimeout = TimeSpan.FromMinutes(5);
        });

        services.Configure<HubOptions>(options =>
        {
            options.SupportedProtocols.Add("test");
            options.HandshakeTimeout = TimeSpan.FromMinutes(10);
        });

        // Act
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<HubOptions<ComponentHub>>>();
        var globalOptions = services.BuildServiceProvider().GetRequiredService<IOptions<HubOptions>>();

        // Assert
        var protocol = Assert.Single(options.Value.SupportedProtocols);
        Assert.Equal(BlazorPackHubProtocol.ProtocolName, protocol);
        Assert.Equal(TimeSpan.FromMinutes(5), options.Value.HandshakeTimeout);

        // Configuring Blazor options is kept separate from the global options.
        Assert.Equal(TimeSpan.FromMinutes(10), globalOptions.Value.HandshakeTimeout);
    }
}

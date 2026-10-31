// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.SignalR.Internal;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests;

public class DefaultHubActivatorTests
{
    public class CreatableHub : Hub
    {
    }

    [Fact]
    public void HubCreatedIfNotResolvedFromServiceProvider()
    {
        Assert.NotNull(
            new DefaultHubActivator<CreatableHub>(Mock.Of<IServiceProvider>()).Create());
    }

    [Fact]
    public void HubCanBeResolvedFromServiceProvider()
    {
        var hub = Mock.Of<Hub>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(Hub)))
            .Returns(hub);

        Assert.Same(hub,
            new DefaultHubActivator<Hub>(mockServiceProvider.Object).Create());
    }

    [Fact]
    public void DisposeNotCalledForHubsResolvedFromServiceProvider()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(Hub)))
            .Returns(() =>
            {
                var m = new Mock<Hub>();
                m.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
                return m.Object;
            });

        var hubActivator = new DefaultHubActivator<Hub>(mockServiceProvider.Object);
        var hub = hubActivator.Create();
        hubActivator.Release(hub);
        Mock.Get(hub).Protected().Verify("Dispose", Times.Never(), ItExpr.IsAny<bool>());
    }

    [Fact]
    public void CannotReleaseNullHub()
    {
        Assert.Equal("hub",
            Assert.Throws<ArgumentNullException>(
                () => new DefaultHubActivator<Hub>(Mock.Of<IServiceProvider>()).Release(null)).ParamName);
    }
}

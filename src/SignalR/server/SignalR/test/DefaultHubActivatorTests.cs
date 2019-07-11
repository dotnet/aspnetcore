// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR.Internal;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class DefaultHubActivatorTests
    {
        public class CreatableHub : Hub
        {
        }

        [Fact]
        public void HubCreatedIfNotResolvedFromServiceProvider()
        {
            var handle = new DefaultHubActivator<CreatableHub>().Create(Mock.Of<IServiceProvider>());
            Assert.NotNull(handle.Hub);
            Assert.True(handle.Created);
        }

        [Fact]
        public void HubCanBeResolvedFromServiceProvider()
        {
            var hub = Mock.Of<CreatableHub>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(CreatableHub)))
                .Returns(hub);
            var handle = new DefaultHubActivator<CreatableHub>().Create(mockServiceProvider.Object);

            Assert.Same(hub, handle.Hub);
            Assert.False(handle.Created);
            Assert.Null(handle.State);
        }


        [Fact]
        public void DisposeNotCalledForHubsResolvedFromServiceProvider()
        {
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(CreatableHub)))
                .Returns(() =>
                {
                    var m = new Mock<CreatableHub>();
                    m.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
                    return m.Object;
                });

            var hubActivator = new DefaultHubActivator<CreatableHub>();
            var handle = hubActivator.Create(mockServiceProvider.Object);
            hubActivator.Release(handle);
            Mock.Get(handle.Hub).Protected().Verify("Dispose", Times.Never(), ItExpr.IsAny<bool>());
        }
    }
}

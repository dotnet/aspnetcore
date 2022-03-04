// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class ClientHubProxyTests
    {
        public class FakeHub : Hub
        {
        }

        [Fact]
        public async Task UserProxy_SendAsync_ArrayArgumentNotExpanded()
        {
            object[] resultArgs = null;

            var o = new Mock<HubLifetimeManager<FakeHub>>();
            o.Setup(m => m.SendUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, object[], CancellationToken>((userId, methodName, args, _) => { resultArgs = args; })
                .Returns(Task.CompletedTask);

            var proxy = new UserProxy<FakeHub>(o.Object, string.Empty);

            var data = Encoding.UTF8.GetBytes("Hello world");
            await proxy.SendAsync("Method", data);

            Assert.NotNull(resultArgs);
            var arg = (byte[]) Assert.Single(resultArgs);

            Assert.Same(data, arg);
        }

        [Fact]
        public async Task MultipleUserProxy_SendAsync_ArrayArgumentNotExpanded()
        {
            object[] resultArgs = null;

            var o = new Mock<HubLifetimeManager<FakeHub>>();
            o.Setup(m => m.SendUsersAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyList<string>, string, object[], CancellationToken>((userIds, methodName, args, _) => { resultArgs = args; })
                .Returns(Task.CompletedTask);

            var proxy = new MultipleUserProxy<FakeHub>(o.Object, new List<string>());

            var data = Encoding.UTF8.GetBytes("Hello world");
            await proxy.SendAsync("Method", data);

            Assert.NotNull(resultArgs);
            var arg = (byte[])Assert.Single(resultArgs);

            Assert.Same(data, arg);
        }

        [Fact]
        public async Task GroupProxy_SendAsync_ArrayArgumentNotExpanded()
        {
            object[] resultArgs = null;

            var o = new Mock<HubLifetimeManager<FakeHub>>();
            o.Setup(m => m.SendGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, object[], CancellationToken>((groupName, methodName, args, _) => { resultArgs = args; })
                .Returns(Task.CompletedTask);

            var proxy = new GroupProxy<FakeHub>(o.Object, string.Empty);

            var data = Encoding.UTF8.GetBytes("Hello world");
            await proxy.SendAsync("Method", data);

            Assert.NotNull(resultArgs);
            var arg = (byte[])Assert.Single(resultArgs);

            Assert.Same(data, arg);
        }

        [Fact]
        public async Task MultipleGroupProxy_SendAsync_ArrayArgumentNotExpanded()
        {
            object[] resultArgs = null;

            var o = new Mock<HubLifetimeManager<FakeHub>>();
            o.Setup(m => m.SendGroupsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyList<string>, string, object[], CancellationToken>((groupNames, methodName, args, _) => { resultArgs = args; })
                .Returns(Task.CompletedTask);

            var proxy = new MultipleGroupProxy<FakeHub>(o.Object, new List<string>());

            var data = Encoding.UTF8.GetBytes("Hello world");
            await proxy.SendAsync("Method", data);

            Assert.NotNull(resultArgs);
            var arg = (byte[])Assert.Single(resultArgs);

            Assert.Same(data, arg);
        }

        [Fact]
        public async Task GroupExceptProxy_SendAsync_ArrayArgumentNotExpanded()
        {
            object[] resultArgs = null;

            var o = new Mock<HubLifetimeManager<FakeHub>>();
            o.Setup(m => m.SendGroupExceptAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, object[], IReadOnlyList<string>, CancellationToken>((groupName, methodName, args, excludedConnectionIds, _) => { resultArgs = args; })
                .Returns(Task.CompletedTask);

            var proxy = new GroupExceptProxy<FakeHub>(o.Object, string.Empty, new List<string>());

            var data = Encoding.UTF8.GetBytes("Hello world");
            await proxy.SendAsync("Method", data);

            Assert.NotNull(resultArgs);
            var arg = (byte[])Assert.Single(resultArgs);

            Assert.Same(data, arg);
        }

        [Fact]
        public async Task AllClientProxy_SendAsync_ArrayArgumentNotExpanded()
        {
            object[] resultArgs = null;

            var o = new Mock<HubLifetimeManager<FakeHub>>();
            o.Setup(m => m.SendAllAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback<string, object[], CancellationToken>((methodName, args, _) => { resultArgs = args; })
                .Returns(Task.CompletedTask);

            var proxy = new AllClientProxy<FakeHub>(o.Object);

            var data = Encoding.UTF8.GetBytes("Hello world");
            await proxy.SendAsync("Method", data);

            Assert.NotNull(resultArgs);
            var arg = (byte[])Assert.Single(resultArgs);

            Assert.Same(data, arg);
        }

        [Fact]
        public async Task AllClientsExceptProxy_SendAsync_ArrayArgumentNotExpanded()
        {
            object[] resultArgs = null;

            var o = new Mock<HubLifetimeManager<FakeHub>>();
            o.Setup(m => m.SendAllExceptAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .Callback<string, object[], IReadOnlyList<string>, CancellationToken>((methodName, args, excludedConnectionIds, _) => { resultArgs = args; })
                .Returns(Task.CompletedTask);

            var proxy = new AllClientsExceptProxy<FakeHub>(o.Object, new List<string>());

            var data = Encoding.UTF8.GetBytes("Hello world");
            await proxy.SendAsync("Method", data);

            Assert.NotNull(resultArgs);
            var arg = (byte[])Assert.Single(resultArgs);

            Assert.Same(data, arg);
        }

        [Fact]
        public async Task SingleClientProxy_SendAsync_ArrayArgumentNotExpanded()
        {
            object[] resultArgs = null;

            var o = new Mock<HubLifetimeManager<FakeHub>>();
            o.Setup(m => m.SendConnectionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, object[], CancellationToken>((connectionId, methodName, args, _) => { resultArgs = args; })
                .Returns(Task.CompletedTask);

            var proxy = new SingleClientProxy<FakeHub>(o.Object, string.Empty);

            var data = Encoding.UTF8.GetBytes("Hello world");
            await proxy.SendAsync("Method", data);

            Assert.NotNull(resultArgs);
            var arg = (byte[])Assert.Single(resultArgs);

            Assert.Same(data, arg);
        }

        [Fact]
        public async Task MultipleClientProxy_SendAsync_ArrayArgumentNotExpanded()
        {
            object[] resultArgs = null;

            var o = new Mock<HubLifetimeManager<FakeHub>>();
            o.Setup(m => m.SendConnectionsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyList<string>, string, object[], CancellationToken>((connectionIds, methodName, args, _) => { resultArgs = args; })
                .Returns(Task.CompletedTask);

            var proxy = new MultipleClientProxy<FakeHub>(o.Object, new List<string>());

            var data = Encoding.UTF8.GetBytes("Hello world");
            await proxy.SendAsync("Method", data);

            Assert.NotNull(resultArgs);
            var arg = (byte[])Assert.Single(resultArgs);

            Assert.Same(data, arg);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests;

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
        var arg = (byte[])Assert.Single(resultArgs);

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

    [Fact]
    public async Task SingleClientProxyWithInvoke_ThrowsNotSupported()
    {
        var hubLifetimeManager = new EmptyHubLifetimeManager<FakeHub>();

        var proxy = new SingleClientProxy<FakeHub>(hubLifetimeManager, "");
        var ex = await Assert.ThrowsAsync<NotImplementedException>(async () => await proxy.InvokeAsync<int>("method", cancellationToken: default)).DefaultTimeout();
        Assert.Equal("EmptyHubLifetimeManager`1 does not support client return values.", ex.Message);
    }

    internal class EmptyHubLifetimeManager<THub> : HubLifetimeManager<THub> where THub : Hub
    {
        public override Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task OnConnectedAsync(HubConnectionContext connection)
        {
            throw new NotImplementedException();
        }

        public override Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            throw new NotImplementedException();
        }

        public override Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task SendAllAsync(string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task SendAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task SendConnectionAsync(string connectionId, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task SendGroupAsync(string groupName, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task SendGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task SendUserAsync(string userId, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

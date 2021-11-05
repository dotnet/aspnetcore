// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

[AttributeUsage(AttributeTargets.Method)]
internal class HubServerProxyAttribute : Attribute
{

}

internal static partial class HubServerProxyExtensions
{
    [HubServerProxy]
    public static partial T GetHubServer<T>(this HubConnection conn);
}

public class HubServerProxyGeneratorTests
{
    public interface IMyHub
    {
        Task GetNothing();
        Task<int> GetScalar();
        Task<List<int>> GetCollection();
        Task<int> SetScalar(int a);
        Task<List<int>> SetCollection(List<int> a);
        Task<ChannelReader<int>> StreamToClientViaChannel();
        Task<ChannelReader<int>> StreamToClientViaChannelWithToken(CancellationToken cancellationToken);
        IAsyncEnumerable<int> StreamToClientViaEnumerableWithToken(CancellationToken cancellationToken);
        Task StreamFromClientViaChannel(ChannelReader<int> reader);
        Task StreamFromClientViaEnumerable(IAsyncEnumerable<int> reader);
        Task<int> StreamFromClientButAlsoReturnValue(ChannelReader<int> reader);
        Task<ChannelReader<int>> StreamBidirectionalViaChannel(ChannelReader<float> reader);
        Task<ChannelReader<int>> StreamBidirectionalViaChannelWithToken(ChannelReader<float> reader, CancellationToken cancellationToken);
        IAsyncEnumerable<int> StreamBidirectionalViaEnumerable(IAsyncEnumerable<float> reader);
        IAsyncEnumerable<int> StreamBidirectionalViaEnumerableWithToken(IAsyncEnumerable<float> reader, CancellationToken cancellationToken);
        ValueTask ReturnValueTask();
        ValueTask<int> ReturnGenericValueTask();
        Task<int?> HandleNullables(float? nullable);
    }

    [Fact]
    public async Task GetNothing()
    {
        // Arrange
        var mockConn = MockHubConnection.Get();
        mockConn
            .Setup(x => x.InvokeCoreAsync(
                nameof(IMyHub.GetNothing),
                typeof(object),
                Array.Empty<object>(),
                default))
            .Returns(Task.FromResult(default(object)));
        var conn = mockConn.Object;
        var myHub = conn.GetHubServer<IMyHub>();

        // Act
        await myHub.GetNothing();

        // Assert
        mockConn.VerifyAll();
    }

    [Fact]
    public async Task GetScalar()
    {
        // Arrange
        var mockConn = MockHubConnection.Get();
        mockConn
            .Setup(x => x.InvokeCoreAsync(
                nameof(IMyHub.GetScalar),
                typeof(int),
                Array.Empty<object>(),
                default))
            .Returns(Task.FromResult((object)10));
        var conn = mockConn.Object;
        var myHub = conn.GetHubServer<IMyHub>();

        // Act
        var result = await myHub.GetScalar();

        // Assert
        mockConn.VerifyAll();
        Assert.Equal(10, result);
    }

    [Fact]
    public async Task GetCollection()
    {
        // Arrange
        var mockConn = MockHubConnection.Get();
        mockConn
            .Setup(x => x.InvokeCoreAsync(
                nameof(IMyHub.GetCollection),
                typeof(List<int>),
                Array.Empty<object>(),
                default))
            .Returns(Task.FromResult((object)new List<int> { 10 }));
        var conn = mockConn.Object;
        var myHub = conn.GetHubServer<IMyHub>();

        // Act
        var result = await myHub.GetCollection();

        // hello

        // Assert
        mockConn.VerifyAll();
        Assert.NotNull(result);
        Assert.Collection(result, item => Assert.Equal(10, item));
    }

    [Fact]
    public async Task SetScalar()
    {
        // Arrange
        var mockConn = MockHubConnection.Get();
        mockConn
            .Setup(x => x.InvokeCoreAsync(
                nameof(IMyHub.SetScalar),
                typeof(int),
                It.Is<object[]>(y => ((object[])y).Any(z => (int)z == 20)),
                default))
            .Returns(Task.FromResult((object)10));
        var conn = mockConn.Object;
        var myHub = conn.GetHubServer<IMyHub>();

        // Act
        var result = await myHub.SetScalar(20);

        // Assert
        mockConn.VerifyAll();
        Assert.Equal(10, result);
    }

    [Fact]
    public async Task SetCollection()
    {
        // Arrange
        var arg = new List<int>() { 20 };
        var mockConn = MockHubConnection.Get();
        mockConn
            .Setup(x => x.InvokeCoreAsync(
                nameof(IMyHub.SetCollection),
                typeof(List<int>),
                It.Is<object[]>(y => ((object[])y).Any(z => (List<int>)z == arg)),
                default))
            .Returns(Task.FromResult((object)new List<int> { 10 }));
        var conn = mockConn.Object;
        var myHub = conn.GetHubServer<IMyHub>();

        // Act
        var result = await myHub.SetCollection(arg);

        // Assert
        mockConn.VerifyAll();
        Assert.NotNull(result);
        Assert.Collection(result, item => Assert.Equal(10, item));
    }

    [Fact]
    public async Task StreamToClient()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<object>();
        var channelForEnumerable = Channel.CreateUnbounded<int>();
        var asyncEnumerable = channelForEnumerable.Reader.ReadAllAsync();
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var mockConn = MockHubConnection.Get();
        mockConn
            .Setup(x => x.StreamAsChannelCoreAsync(
                nameof(IMyHub.StreamToClientViaChannel),
                typeof(int),
                Array.Empty<object>(),
                default))
            .Returns(Task.FromResult(channel.Reader));
        mockConn
            .Setup(x => x.StreamAsChannelCoreAsync(
                nameof(IMyHub.StreamToClientViaChannelWithToken),
                typeof(int),
                Array.Empty<object>(),
                token))
            .Returns(Task.FromResult(channel.Reader));
        mockConn
            .Setup(x => x.StreamAsyncCore<int>(
                nameof(IMyHub.StreamToClientViaEnumerableWithToken),
                Array.Empty<object>(),
                token))
            .Returns(asyncEnumerable);
        var conn = mockConn.Object;
        var myHub = conn.GetHubServer<IMyHub>();

        // Act
        _ = await myHub.StreamToClientViaChannel();
        _ = await myHub.StreamToClientViaChannelWithToken(token);
        _ = myHub.StreamToClientViaEnumerableWithToken(token);

        // Assert
        mockConn.VerifyAll();
    }

    [Fact]
    public async Task StreamFromClient()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<int>();
        var channelReader = channel.Reader;
        var channelForEnumerable = Channel.CreateUnbounded<int>();
        var asyncEnumerable = channelForEnumerable.Reader.ReadAllAsync();
        var mockConn = MockHubConnection.Get();
        mockConn
            .Setup(x => x.SendCoreAsync(
                nameof(IMyHub.StreamFromClientViaChannel),
                It.Is<object[]>(y => ((object[])y).Any(z => (ChannelReader<int>)z == channelReader)),
                default))
            .Returns(Task.CompletedTask);
        mockConn
            .Setup(x => x.SendCoreAsync(
                nameof(IMyHub.StreamFromClientViaEnumerable),
                It.Is<object[]>(y => ((object[])y).Any(z => (IAsyncEnumerable<int>)z == asyncEnumerable)),
                default))
            .Returns(Task.CompletedTask);
        mockConn
            .Setup(x => x.InvokeCoreAsync(
                nameof(IMyHub.StreamFromClientButAlsoReturnValue),
                typeof(int),
                It.Is<object[]>(y => ((object[])y).Any(z => (ChannelReader<int>)z == channelReader)),
                default))
            .Returns(Task.FromResult((object)6));
        var conn = mockConn.Object;
        var myHub = conn.GetHubServer<IMyHub>();

        // Act
        await myHub.StreamFromClientViaChannel(channelReader);
        await myHub.StreamFromClientViaEnumerable(asyncEnumerable);
        var result = await myHub.StreamFromClientButAlsoReturnValue(channelReader);

        // Assert
        mockConn.VerifyAll();
        Assert.Equal(6, result);
    }

    [Fact]
    public async Task BidirectionalStream()
    {
        // Arrange
        var argChannel = Channel.CreateUnbounded<float>();
        var retChannel = Channel.CreateUnbounded<object>();
        var retChannelReader = retChannel.Reader;
        var argChannelForEnumerable = Channel.CreateUnbounded<float>();
        var argEnumerable = argChannelForEnumerable.Reader.ReadAllAsync();
        var retChannelForEnumerable = Channel.CreateUnbounded<int>();
        var retEnumerable = retChannelForEnumerable.Reader.ReadAllAsync();
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var mockConn = MockHubConnection.Get();
        mockConn
            .Setup(x => x.StreamAsChannelCoreAsync(
                nameof(IMyHub.StreamBidirectionalViaChannel),
                typeof(int),
                It.Is<object[]>(y => ((object[])y).Any(z => z is ChannelReader<float>)),
                default))
            .Returns(Task.FromResult(retChannelReader));
        mockConn
            .Setup(x => x.StreamAsChannelCoreAsync(
                nameof(IMyHub.StreamBidirectionalViaChannelWithToken),
                typeof(int),
                It.Is<object[]>(y => ((object[])y).Any(z => z is ChannelReader<float>)),
                token))
            .Returns(Task.FromResult(retChannelReader));
        mockConn
            .Setup(x => x.StreamAsyncCore<int>(
                nameof(IMyHub.StreamBidirectionalViaEnumerable),
                It.Is<object[]>(y => ((object[])y).Any(z => z is IAsyncEnumerable<float>)),
                default))
            .Returns(retEnumerable);
        mockConn
            .Setup(x => x.StreamAsyncCore<int>(
                nameof(IMyHub.StreamBidirectionalViaEnumerableWithToken),
                It.Is<object[]>(y => ((object[])y).Any(z => z is IAsyncEnumerable<float>)),
                token))
            .Returns(retEnumerable);
        var conn = mockConn.Object;
        var myHub = conn.GetHubServer<IMyHub>();

        // Act
        _ = await myHub.StreamBidirectionalViaChannel(argChannel.Reader);
        _ = await myHub.StreamBidirectionalViaChannelWithToken(argChannel.Reader, token);
        _ = myHub.StreamBidirectionalViaEnumerable(argEnumerable);
        _ = myHub.StreamBidirectionalViaEnumerableWithToken(argEnumerable, token);

        // Assert
        mockConn.VerifyAll();
    }

    [Fact]
    public async Task ReturnValueTask()
    {
        // Arrange
        var mockConn = MockHubConnection.Get();
        mockConn
            .Setup(x => x.InvokeCoreAsync(
                nameof(IMyHub.ReturnValueTask),
                typeof(object),
                Array.Empty<object>(),
                default))
            .Returns(Task.FromResult(default(object)));
        mockConn
            .Setup(x => x.InvokeCoreAsync(
                nameof(IMyHub.ReturnGenericValueTask),
                typeof(int),
                Array.Empty<object>(),
                default))
            .Returns(Task.FromResult((object)10));
        var conn = mockConn.Object;
        var myHub = conn.GetHubServer<IMyHub>();

        // Act
        await myHub.ReturnValueTask();
        var result = await myHub.ReturnGenericValueTask();

        // Assert
        mockConn.VerifyAll();
        Assert.Equal(10, result);
    }
}

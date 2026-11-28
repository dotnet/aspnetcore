// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

public partial class MessagingTest
{
    [Fact]
    public async Task Constructors_AreStableAndCreateDistinctCallerOwnedObjects()
    {
        var runtime = new Mock<IJSRuntime>();
        runtime.Setup(r => r.InvokeConstructorAsync("MessageChannel", It.IsAny<object?[]?>()))
            .Returns(() => ValueTask.FromResult(new Mock<IJSObjectReference>().Object));
        runtime.Setup(r => r.InvokeConstructorAsync("BroadcastChannel", It.IsAny<object?[]?>()))
            .Returns(() => ValueTask.FromResult(new Mock<IJSObjectReference>().Object));
        await using var provider = CreateProvider(runtime.Object);
        var window = provider.GetRequiredService<IBrowserPlatform>().Window;

        Assert.Same(window.MessageChannel, window.MessageChannel);
        Assert.Same(window.BroadcastChannel, window.BroadcastChannel);
        Assert.Empty(runtime.Invocations);
        await using var first = await window.MessageChannel.CreateAsync();
        await using var second = await window.MessageChannel.CreateAsync();
        await using var broadcast = await window.BroadcastChannel.CreateAsync("name");
        Assert.NotSame(first, second);
        Assert.Same(first.Port1, first.Port1);
        Assert.Same(first.Port2, first.Port2);
        Assert.NotSame(first.Port1, first.Port2);
        runtime.Verify(r => r.InvokeConstructorAsync("BroadcastChannel", new object?[] { "name" }), Times.Once);
        await Assert.ThrowsAsync<ArgumentNullException>(() => window.BroadcastChannel.CreateAsync(null!).AsTask());
    }

    [Fact]
    public async Task Ports_SharePendingAcquisitionsAndRetryFailures()
    {
        var runtime = new Mock<IJSRuntime>();
        var channelReference = new Mock<IJSObjectReference>();
        var portReference = new Mock<IJSObjectReference>();
        var pending = new TaskCompletionSource<IJSObjectReference>();
        channelReference.SetupSequence(r => r.GetValueAsync<IJSObjectReference>("port1"))
            .ThrowsAsync(new JSException("temporary failure"))
            .Returns(new ValueTask<IJSObjectReference>(Task.FromCanceled<IJSObjectReference>(new CancellationToken(canceled: true))))
            .Returns(new ValueTask<IJSObjectReference>(pending.Task));
        runtime.Setup(r => r.InvokeConstructorAsync("MessageChannel", It.IsAny<object?[]?>()))
            .ReturnsAsync(channelReference.Object);
        await using var provider = CreateProvider(runtime.Object);
        await using var channel = await provider.GetRequiredService<IBrowserPlatform>().Window.MessageChannel.CreateAsync();

        Assert.Empty(channelReference.Invocations);
        await Assert.ThrowsAsync<JSException>(() => channel.Port1.StartAsync().AsTask());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => channel.Port1.StartAsync().AsTask());
        var first = channel.Port1.StartAsync().AsTask();
        var second = channel.Port1.StartAsync().AsTask();
        pending.SetResult(portReference.Object);
        await Task.WhenAll(first, second);

        channelReference.Verify(r => r.GetValueAsync<IJSObjectReference>("port1"), Times.Exactly(3));
        channelReference.Verify(r => r.GetValueAsync<IJSObjectReference>("port2"), Times.Never);
        portReference.Verify(r => r.InvokeAsync<It.IsAnyType>("start", It.IsAny<object?[]?>()), Times.Exactly(2));
        await channel.Port1.CloseAsync();
        portReference.Verify(r => r.InvokeAsync<It.IsAnyType>("close", It.IsAny<object?[]?>()), Times.Once);
    }

    [Fact]
    public async Task ChannelDisposal_DoesNotWaitForPendingPortAndInvalidatesBothPorts()
    {
        var runtime = new Mock<IJSRuntime>();
        var reference = new Mock<IJSObjectReference>();
        reference.Setup(r => r.GetValueAsync<IJSObjectReference>("port1"))
            .Returns(new ValueTask<IJSObjectReference>(new TaskCompletionSource<IJSObjectReference>().Task));
        runtime.Setup(r => r.InvokeConstructorAsync("MessageChannel", It.IsAny<object?[]?>()))
            .ReturnsAsync(reference.Object);
        await using var provider = CreateProvider(runtime.Object);
        var channel = await provider.GetRequiredService<IBrowserPlatform>().Window.MessageChannel.CreateAsync();
        var port1 = channel.Port1;
        var port2 = channel.Port2;
        _ = port1.StartAsync();

        await channel.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        await channel.DisposeAsync();

        reference.Verify(r => r.DisposeAsync(), Times.Once);
        reference.Verify(r => r.GetValueAsync<IJSObjectReference>("port2"), Times.Never);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => port1.StartAsync().AsTask());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => port2.CloseAsync().AsTask());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => port2.PostMessageAsync(default(JsonElement)).AsTask());
        Assert.Throws<ObjectDisposedException>(() => channel.Port1);
        Assert.Throws<ObjectDisposedException>(() => channel.Port2);
    }

    [Fact]
    public async Task ChannelDisposal_ReleasesBothPortsAndChannelWhenCloseThrows()
    {
        var runtime = new Mock<IJSRuntime>();
        var reference = new Mock<IJSObjectReference>();
        var port1 = new Mock<IJSObjectReference>();
        var port2 = new Mock<IJSObjectReference>();
        reference.Setup(r => r.GetValueAsync<IJSObjectReference>("port1")).ReturnsAsync(port1.Object);
        reference.Setup(r => r.GetValueAsync<IJSObjectReference>("port2")).ReturnsAsync(port2.Object);
        runtime.Setup(r => r.InvokeConstructorAsync("MessageChannel", It.IsAny<object?[]?>()))
            .ReturnsAsync(reference.Object);
        await using var provider = CreateProvider(runtime.Object);
        var channel = await provider.GetRequiredService<IBrowserPlatform>().Window.MessageChannel.CreateAsync();
        await channel.Port1.StartAsync();
        await channel.Port2.StartAsync();
        var expected = new JSDisconnectedException("disconnected");
        port1.Setup(r => r.InvokeAsync<It.IsAnyType>("close", It.IsAny<object?[]?>())).Throws(expected);

        var actual = await Assert.ThrowsAsync<JSDisconnectedException>(() => channel.DisposeAsync().AsTask());

        Assert.Same(expected, actual);
        port1.Verify(r => r.InvokeAsync<It.IsAnyType>("close", It.IsAny<object?[]?>()), Times.Once);
        port2.Verify(r => r.InvokeAsync<It.IsAnyType>("close", It.IsAny<object?[]?>()), Times.Once);
        port1.Verify(r => r.DisposeAsync(), Times.Once);
        port2.Verify(r => r.DisposeAsync(), Times.Once);
        reference.Verify(r => r.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task BroadcastChannel_ForwardsNameAndReleasesReferenceWhenCloseThrows()
    {
        var runtime = new Mock<IJSRuntime>();
        var reference = new Mock<IJSObjectReference>();
        reference.Setup(r => r.GetValueAsync<string>("name")).ReturnsAsync("channel-name");
        runtime.Setup(r => r.InvokeConstructorAsync("BroadcastChannel", It.IsAny<object?[]?>()))
            .ReturnsAsync(reference.Object);
        await using var provider = CreateProvider(runtime.Object);
        var channel = await provider.GetRequiredService<IBrowserPlatform>().Window.BroadcastChannel.CreateAsync("channel-name");
        Assert.Equal("channel-name", await channel.GetNameAsync());
        await channel.CloseAsync();
        reference.Verify(r => r.InvokeAsync<It.IsAnyType>("close", It.IsAny<object?[]?>()), Times.Once);
        var expected = new JSDisconnectedException("disconnected");
        reference.Setup(r => r.InvokeAsync<It.IsAnyType>("close", It.IsAny<object?[]?>())).Throws(expected);

        Assert.Same(expected, await Assert.ThrowsAsync<JSDisconnectedException>(() => channel.DisposeAsync().AsTask()));
        await channel.DisposeAsync();

        reference.Verify(r => r.DisposeAsync(), Times.Once);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => channel.GetNameAsync().AsTask());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => channel.CloseAsync().AsTask());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => channel.PostMessageAsync(default(JsonElement)).AsTask());
    }

    [Fact]
    public async Task PostMessage_RejectsUndefinedAndDisposedJson()
    {
        var runtime = new Mock<IJSRuntime>();
        var channelReference = new Mock<IJSObjectReference>();
        var portReference = new Mock<IJSObjectReference>();
        channelReference.Setup(r => r.GetValueAsync<IJSObjectReference>("port1")).ReturnsAsync(portReference.Object);
        runtime.Setup(r => r.InvokeConstructorAsync("MessageChannel", It.IsAny<object?[]?>())).ReturnsAsync(channelReference.Object);
        runtime.Setup(r => r.InvokeConstructorAsync("BroadcastChannel", It.IsAny<object?[]?>())).ReturnsAsync(channelReference.Object);
        await using var provider = CreateProvider(runtime.Object);
        var window = provider.GetRequiredService<IBrowserPlatform>().Window;
        await using var channel = await window.MessageChannel.CreateAsync();
        await using var broadcast = await window.BroadcastChannel.CreateAsync("name");
        var document = JsonDocument.Parse("null");
        var message = document.RootElement;
        document.Dispose();

        await Assert.ThrowsAsync<InvalidOperationException>(() => channel.Port1.PostMessageAsync(default(JsonElement)).AsTask());
        await Assert.ThrowsAsync<InvalidOperationException>(() => broadcast.PostMessageAsync(default(JsonElement)).AsTask());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => channel.Port1.PostMessageAsync(message).AsTask());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => broadcast.PostMessageAsync(message).AsTask());
        runtime.Verify(r => r.InvokeAsync<IJSObjectReference>("JSON.parse", It.IsAny<object?[]?>()), Times.Never);
        portReference.Verify(r => r.InvokeAsync<It.IsAnyType>("postMessage.apply", It.IsAny<object?[]?>()), Times.Never);
        channelReference.Verify(r => r.InvokeAsync<It.IsAnyType>("postMessage.apply", It.IsAny<object?[]?>()), Times.Never);
    }

    [Fact]
    public async Task PostMessage_ParsesJsonAsDataAndReleasesArgumentsOnFailure()
    {
        var runtime = new Mock<IJSRuntime>();
        var channelReference = new Mock<IJSObjectReference>();
        var argumentsReference = new Mock<IJSObjectReference>();
        runtime.Setup(r => r.InvokeConstructorAsync("BroadcastChannel", It.IsAny<object?[]?>()))
            .ReturnsAsync(channelReference.Object);
        runtime.Setup(r => r.InvokeAsync<IJSObjectReference>("JSON.parse", It.IsAny<object?[]?>()))
            .ReturnsAsync(argumentsReference.Object);
        await using var provider = CreateProvider(runtime.Object);
        await using var channel = await provider.GetRequiredService<IBrowserPlatform>().Window.BroadcastChannel.CreateAsync("name");
        using var document = JsonDocument.Parse("""{"hasOwnProperty":"value","__jsObjectId":"data"}""");
        var expected = new JSException("post failed");
        channelReference.Setup(r => r.InvokeAsync<It.IsAnyType>("postMessage.apply", It.IsAny<object?[]?>())).Throws(expected);

        Assert.Same(expected, await Assert.ThrowsAsync<JSException>(() => channel.PostMessageAsync(document.RootElement).AsTask()));

        runtime.Verify(r => r.InvokeAsync<IJSObjectReference>("JSON.parse",
            new object?[] { """[{"hasOwnProperty":"value","__jsObjectId":"data"}]""" }), Times.Once);
        channelReference.Verify(r => r.InvokeAsync<It.IsAnyType>("postMessage.apply",
            new object?[] { channelReference.Object, argumentsReference.Object }), Times.Once);
        argumentsReference.Verify(r => r.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task PostMessage_ForwardsStringsAndSerializesWithSuppliedMetadata()
    {
        var runtime = new Mock<IJSRuntime>();
        var channelReference = new Mock<IJSObjectReference>();
        var portReference = new Mock<IJSObjectReference>();
        var argumentsReference = new Mock<IJSObjectReference>();
        channelReference.Setup(r => r.GetValueAsync<IJSObjectReference>("port1")).ReturnsAsync(portReference.Object);
        runtime.Setup(r => r.InvokeConstructorAsync("MessageChannel", It.IsAny<object?[]?>())).ReturnsAsync(channelReference.Object);
        runtime.Setup(r => r.InvokeConstructorAsync("BroadcastChannel", It.IsAny<object?[]?>())).ReturnsAsync(channelReference.Object);
        var jsonArguments = new List<string>();
        runtime.Setup(r => r.InvokeAsync<IJSObjectReference>("JSON.parse", It.IsAny<object?[]?>()))
            .Callback<string, object?[]?>((_, args) => jsonArguments.Add(Assert.IsType<string>(Assert.Single(args!))))
            .ReturnsAsync(argumentsReference.Object);
        await using var provider = CreateProvider(runtime.Object);
        var window = provider.GetRequiredService<IBrowserPlatform>().Window;
        await using var channel = await window.MessageChannel.CreateAsync();
        await using var broadcast = await window.BroadcastChannel.CreateAsync("name");
        const string text = """{"kind":"not parsed","__jsObjectId":1}""";
        var message = new TestMessage { Kind = "refresh", Version = 2 };

        await channel.Port1.PostMessageAsync(text);
        await broadcast.PostMessageAsync(text);
        Assert.Empty(jsonArguments);
        portReference.Verify(r => r.InvokeAsync<It.IsAnyType>("postMessage", new object?[] { text }), Times.Once);
        channelReference.Verify(r => r.InvokeAsync<It.IsAnyType>("postMessage", new object?[] { text }), Times.Once);

        await channel.Port1.PostMessageAsync(message, TestJsonContext.Default.TestMessage);
        await broadcast.PostMessageAsync(message, TestJsonContext.Default.TestMessage);
        await channel.Port1.PostMessageAsync(new List<int> { 1, 2 }, TestJsonContext.Default.ListInt32);
        await broadcast.PostMessageAsync(new List<int> { 1, 2 }, TestJsonContext.Default.ListInt32);
        await channel.Port1.PostMessageAsync(null, TestJsonContext.Default.NullableInt32);
        await broadcast.PostMessageAsync(null, TestJsonContext.Default.NullableInt32);

        Assert.Equal(
            ["""[{"kind":"refresh","version":2}]""", """[{"kind":"refresh","version":2}]""", "[[1,2]]", "[[1,2]]", "[null]", "[null]"],
            jsonArguments);
        argumentsReference.Verify(r => r.DisposeAsync(), Times.Exactly(6));
    }

    [Fact]
    public async Task PostMessage_RejectsInvalidArgumentsAndDisposedTargets()
    {
        var runtime = new Mock<IJSRuntime>();
        var reference = new Mock<IJSObjectReference>();
        runtime.Setup(r => r.InvokeConstructorAsync("MessageChannel", It.IsAny<object?[]?>())).ReturnsAsync(reference.Object);
        runtime.Setup(r => r.InvokeConstructorAsync("BroadcastChannel", It.IsAny<object?[]?>())).ReturnsAsync(reference.Object);
        await using var provider = CreateProvider(runtime.Object);
        var window = provider.GetRequiredService<IBrowserPlatform>().Window;
        var channel = await window.MessageChannel.CreateAsync();
        var port = channel.Port1;
        var broadcast = await window.BroadcastChannel.CreateAsync("name");

        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() => port.PostMessageAsync((string)null!).AsTask())).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() => broadcast.PostMessageAsync((string)null!).AsTask())).ParamName);
        Assert.Equal("jsonTypeInfo", (await Assert.ThrowsAsync<ArgumentNullException>(() => port.PostMessageAsync(1, null!).AsTask())).ParamName);
        Assert.Equal("jsonTypeInfo", (await Assert.ThrowsAsync<ArgumentNullException>(() => broadcast.PostMessageAsync(1, null!).AsTask())).ParamName);
        await Assert.ThrowsAsync<ArgumentException>(() => port.PostMessageAsync(double.NaN, TestJsonContext.Default.Double).AsTask());
        await Assert.ThrowsAsync<ArgumentException>(() => broadcast.PostMessageAsync(double.NaN, TestJsonContext.Default.Double).AsTask());
        Assert.Empty(reference.Invocations);
        runtime.Verify(r => r.InvokeAsync<IJSObjectReference>("JSON.parse", It.IsAny<object?[]?>()), Times.Never);

        await channel.DisposeAsync();
        await broadcast.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => port.PostMessageAsync("text").AsTask());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => broadcast.PostMessageAsync("text").AsTask());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => port.PostMessageAsync(1, TestJsonContext.Default.NullableInt32).AsTask());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => broadcast.PostMessageAsync(1, TestJsonContext.Default.NullableInt32).AsTask());
    }

    public sealed class TestMessage
    {
        public required string Kind { get; init; }
        public int Version { get; init; }
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(TestMessage))]
    [JsonSerializable(typeof(List<int>))]
    [JsonSerializable(typeof(int?))]
    [JsonSerializable(typeof(double))]
    private partial class TestJsonContext : JsonSerializerContext
    {
    }

    private static ServiceProvider CreateProvider(IJSRuntime runtime)
    {
        var services = new ServiceCollection();
        services.AddSingleton(runtime);
        services.AddBrowserPlatform();

        return services.BuildServiceProvider();
    }
}

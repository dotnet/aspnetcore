// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR.Protocol;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests.Internal;

public class StreamTrackerTests
{
    [Fact]
    public async Task StreamsOfSameItemTypeGetIndependentChannels()
    {
        var tracker = new StreamTracker(streamBufferCapacity: 10);

        var first = Assert.IsAssignableFrom<ChannelReader<string>>(
            tracker.AddStream("1", typeof(string), typeof(ChannelReader<string>), tracker.GetNextStreamOwner()));
        var second = Assert.IsAssignableFrom<ChannelReader<string>>(
            tracker.AddStream("2", typeof(string), typeof(ChannelReader<string>), tracker.GetNextStreamOwner()));
        Assert.NotSame(first, second);

        Assert.True(tracker.TryProcessItem(new StreamItemMessage("1", "a"), out var write));
        await write.DefaultTimeout();
        Assert.True(tracker.TryComplete(CompletionMessage.Empty("1")));

        Assert.Equal("a", await first.ReadAsync().AsTask().DefaultTimeout());
        await first.Completion.DefaultTimeout();
        Assert.False(second.TryRead(out _));
        Assert.False(second.Completion.IsCompleted);
    }

    [Fact]
    public async Task StreamsUseRequestedItemTypeAndTargetType()
    {
        var tracker = new StreamTracker(streamBufferCapacity: 10);

        var ints = Assert.IsAssignableFrom<IAsyncEnumerable<int>>(
            tracker.AddStream("ints", typeof(int), typeof(IAsyncEnumerable<int>), tracker.GetNextStreamOwner()));
        var strings = Assert.IsAssignableFrom<ChannelReader<string>>(
            tracker.AddStream("strings", typeof(string), typeof(ChannelReader<string>), tracker.GetNextStreamOwner()));

        Assert.Equal(typeof(int), tracker.GetStreamItemType("ints"));
        Assert.Equal(typeof(string), tracker.GetStreamItemType("strings"));

        Assert.True(tracker.TryProcessItem(new StreamItemMessage("ints", 42), out var write));
        await write.DefaultTimeout();
        Assert.True(tracker.TryComplete(CompletionMessage.Empty("ints")));

        var received = new List<int>();
        await foreach (var item in ints)
        {
            received.Add(item);
        }
        Assert.Equal(new[] { 42 }, received);
        Assert.False(strings.Completion.IsCompleted);
    }

    [Fact]
    public void DuplicateStreamIdThrows()
    {
        var tracker = new StreamTracker(streamBufferCapacity: 10);
        tracker.AddStream("1", typeof(string), typeof(ChannelReader<string>), tracker.GetNextStreamOwner());

        var ex = Assert.Throws<HubException>(() =>
            tracker.AddStream("1", typeof(string), typeof(ChannelReader<string>), tracker.GetNextStreamOwner()));
        Assert.Equal("Stream ID '1' is already in use.", ex.Message);
    }
}

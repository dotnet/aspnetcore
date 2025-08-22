// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests;

public class SerializedHubMessageTests
{
    [Fact]
    public void GetSerializedMessageSerializesUsingHubProtocolIfNoCacheAvailable()
    {
        var invocation = new InvocationMessage("Foo", new object[0]);
        var message = new SerializedHubMessage(invocation);
        var protocol = new DummyHubProtocol("p1");

        var serialized = message.GetSerializedMessage(protocol);

        Assert.Equal(DummyHubProtocol.DummySerialization, serialized.ToArray());
        Assert.Collection(protocol.GetWrittenMessages(),
            actualMessage => Assert.Same(invocation, actualMessage));
    }

    [Fact]
    public void GetSerializedMessageReturnsCachedSerializationIfAvailable()
    {
        var invocation = new InvocationMessage("Foo", new object[0]);
        var message = new SerializedHubMessage(invocation);
        var protocol = new DummyHubProtocol("p1");

        // This should cache it
        _ = message.GetSerializedMessage(protocol);

        // Get it again
        var serialized = message.GetSerializedMessage(protocol);

        Assert.Equal(DummyHubProtocol.DummySerialization, serialized.ToArray());

        // We should still only have written one message
        Assert.Collection(protocol.GetWrittenMessages(),
            actualMessage => Assert.Same(invocation, actualMessage));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public async Task SerializingTwoMessagesFromTheSameProtocolSimultaneouslyResultsInOneCachedItemAsync(int numberOfSerializationsToPreCache)
    {
        var invocation = new InvocationMessage("Foo", new object[0]);
        var message = new SerializedHubMessage(invocation);

        // "Pre-cache" the requested number of serializations (so we can test scenarios involving each of the fields and the fallback list)
        for (var i = 0; i < numberOfSerializationsToPreCache; i++)
        {
            _ = message.GetSerializedMessage(new DummyHubProtocol($"p{i}"));
        }

        var onWrite = SyncPoint.Create(2, out var syncPoints);
        var protocol = new DummyHubProtocol("test", () => onWrite().Wait());

        // Serialize once, but hold at the Hub Protocol
        var firstSerialization = Task.Run(() => message.GetSerializedMessage(protocol));
        await syncPoints[0].WaitForSyncPoint();

        // Serialize again, which should hit the lock
        var secondSerialization = Task.Run(() => message.GetSerializedMessage(protocol));
        Assert.False(secondSerialization.IsCompleted);

        // Release both instances of the syncpoint
        syncPoints[0].Continue();
        syncPoints[1].Continue();

        // Everything should finish and only one serialization should be written
        await firstSerialization.DefaultTimeout();
        await secondSerialization.DefaultTimeout();

        Assert.Collection(message.GetAllSerializations().Skip(numberOfSerializationsToPreCache).ToArray(),
            serializedMessage =>
            {
                Assert.Equal("test", serializedMessage.ProtocolName);
                Assert.Equal(DummyHubProtocol.DummySerialization, serializedMessage.Serialized.ToArray());
            });
    }
}

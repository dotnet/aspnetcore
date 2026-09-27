// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

public class ContentLoggingStreamTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task ForwardsEveryByteAndLeavesDestinationOpen(int overload)
    {
        using var destination = new MemoryStream();
        var sink = new TestSink();
        var logger = new TestLogger("test", sink, enabled: true);
        var data = Encoding.UTF8.GetBytes(new string('a', 1500));
        var original = data.ToArray();
        using (var stream = new ContentLoggingStream(destination, logger))
        {
            switch (overload)
            {
                case 0:
                    stream.Write(data, 0, data.Length);
                    break;
                case 1:
                    await stream.WriteAsync(data, 0, data.Length);
                    break;
                case 2:
                    stream.Write(data.AsSpan());
                    break;
                default:
                    await stream.WriteAsync(data.AsMemory());
                    break;
            }
        }
        data.AsSpan().Fill((byte)'b');

        Assert.True(destination.CanWrite);
        Assert.Equal(original, destination.ToArray());
        var entry = Assert.Single(sink.Writes);
        var fields = ((IEnumerable<KeyValuePair<string, object>>)entry.State).ToDictionary(pair => pair.Key, pair => pair.Value);
        Assert.Equal(1500L, fields["ByteCount"]);
        Assert.Equal(1024, fields["LoggedByteCount"]);
        Assert.Equal(true, fields["Truncated"]);
        Assert.Equal(new string('a', 1024), fields["Content"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanceledWriteDoesNotLogOrWrite(bool memoryOverload)
    {
        using var destination = new MemoryStream();
        var sink = new TestSink();
        using var stream = new ContentLoggingStream(destination, new TestLogger("test", sink, enabled: true));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        byte[] data = [1, 2, 3];

        if (memoryOverload)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stream.WriteAsync(data.AsMemory(), cancellation.Token).AsTask());
        }
        else
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stream.WriteAsync(data, 0, data.Length, cancellation.Token));
        }
        Assert.Empty(sink.Writes);
        Assert.Equal(0, destination.Length);
    }
}

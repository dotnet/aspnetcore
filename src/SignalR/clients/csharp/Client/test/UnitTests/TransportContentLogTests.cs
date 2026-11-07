// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

public class TransportContentLogTests
{
    [Fact]
    public void ContentLoggingIsDisabledByDefault()
    {
        Assert.False(new HttpConnectionOptions().LogMessageContent);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void DoesNotReadContentWhenDisabled(bool enabled, bool traceEnabled)
    {
        var sink = new TestSink();
        var logger = new TestLogger("test", sink, level => traceEnabled);
        using var memory = new UnreadableMemory();
        var data = new ReadOnlySequence<byte>(memory.ExposeMemory());

        TransportContentLog.Write(logger, enabled, received: true, data);

        Assert.Empty(sink.Writes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1023)]
    [InlineData(1024)]
    [InlineData(1025)]
    [InlineData(1500)]
    public void RecordsOriginalLengthAndTruncationWithoutChangingData(int length)
    {
        var data = Enumerable.Repeat((byte)'a', length).ToArray();
        var original = data.ToArray();
        var sink = new TestSink();
        var logger = new TestLogger("test", sink, enabled: true);

        TransportContentLog.Write(logger, enabled: true, received: true, data.AsSpan());

        Assert.Equal(original, data);
        if (length == 0)
        {
            Assert.Empty(sink.Writes);
            return;
        }
        var write = Assert.Single(sink.Writes);
        var values = Values(write);
        Assert.Equal((long)length, values["ByteCount"]);
        Assert.Equal(Math.Min(length, 1024), values["LoggedByteCount"]);
        Assert.Equal(length > 1024, values["Truncated"]);
        Assert.Equal(new string('a', Math.Min(length, 1024)), values["Content"]);
        Assert.Equal(LogLevel.Trace, write.LogLevel);
    }

    [Theory]
    [InlineData(true, "ReceivedMessageContent")]
    [InlineData(false, "SendingMessageContent")]
    public void LoggingStateSurvivesReusingSegmentedBuffers(bool received, string eventName)
    {
        var first = Encoding.UTF8.GetBytes("Hel");
        var second = Encoding.UTF8.GetBytes("lo");
        var head = new Segment(first);
        var tail = head.Append(second);
        var data = new ReadOnlySequence<byte>(head, 0, tail, tail.Memory.Length);
        var sink = new TestSink();
        var logger = new TestLogger("test", sink, enabled: true);

        TransportContentLog.Write(logger, enabled: true, received, data);
        first.AsSpan().Fill((byte)'x');
        second.AsSpan().Fill((byte)'y');

        // TestLogger stores the state and formatter, without formatting the message.
        var write = Assert.Single(sink.Writes);
        Assert.Equal(eventName, write.EventId.Name);
        Assert.Equal("Hello", Values(write)["Content"]);
        Assert.Contains("Hello", write.Message);
    }

    [Fact]
    public void EscapesBinaryAndPartialUtf8WithoutLosingBytes()
    {
        var sink = new TestSink();
        var logger = new TestLogger("test", sink, enabled: true);
        byte[] first = [(byte)'A', (byte)'\\', 0xE4, 0xB8];
        byte[] second = [0xAD, 0x00, 0xFF, (byte)'\n'];

        TransportContentLog.Write(logger, enabled: true, received: true, first.AsSpan());
        TransportContentLog.Write(logger, enabled: true, received: true, second.AsSpan());

        var writes = sink.Writes.ToArray();
        Assert.Equal(2, writes.Length);
        Assert.Equal(@"A\\\xE4\xB8", Values(writes[0])["Content"]);
        Assert.Equal(@"\xAD\x00\xFF\x0A", Values(writes[1])["Content"]);
    }

    [Fact]
    public void TruncatesAcrossSegmentsWithoutReadingTheRemainder()
    {
        var head = new Segment(Enumerable.Repeat((byte)'a', 1023).ToArray());
        var middle = head.Append(new byte[] { (byte)'b' });
        using var remainder = new UnreadableMemory();
        var tail = middle.Append(remainder.ExposeMemory());
        var data = new ReadOnlySequence<byte>(head, 0, tail, tail.Memory.Length);
        var sink = new TestSink();
        var logger = new TestLogger("test", sink, enabled: true);

        TransportContentLog.Write(logger, enabled: true, received: false, data);

        var values = Values(Assert.Single(sink.Writes));
        Assert.Equal(new string('a', 1023) + "b", values["Content"]);
        Assert.Equal(1025L, values["ByteCount"]);
        Assert.Equal(true, values["Truncated"]);
    }

    private static Dictionary<string, object> Values(WriteContext write) =>
        ((IEnumerable<KeyValuePair<string, object>>)write.State).ToDictionary(pair => pair.Key, pair => pair.Value);

    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }

    private sealed class UnreadableMemory : MemoryManager<byte>
    {
        public Memory<byte> ExposeMemory() => CreateMemory(1);
        public override Span<byte> GetSpan() => throw new InvalidOperationException("Content should not have been read.");
        public override MemoryHandle Pin(int elementIndex = 0) => throw new NotSupportedException();
        public override void Unpin() => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { }
    }
}

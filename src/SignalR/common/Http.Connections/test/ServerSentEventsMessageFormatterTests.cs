// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Http.Connections.Tests;

public class ServerSentEventsMessageFormatterTests
{
    [Theory]
    [MemberData(nameof(PayloadData))]
    public async Task WriteTextMessageFromSingleSegment(string encoded, string payload)
    {
        var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(payload));

        var output = new MemoryStream();
        await ServerSentEventsMessageFormatter.WriteMessageAsync(buffer, output, default);

        Assert.Equal(encoded, Encoding.UTF8.GetString(output.ToArray()));
    }

    [Theory]
    [MemberData(nameof(PayloadData))]
    public async Task WriteTextMessageFromMultipleSegments(string encoded, string payload)
    {
        var buffer = ReadOnlySequenceFactory.SegmentPerByteFactory.CreateWithContent(Encoding.UTF8.GetBytes(payload));

        var output = new MemoryStream();
        await ServerSentEventsMessageFormatter.WriteMessageAsync(buffer, output, default);

        Assert.Equal(encoded, Encoding.UTF8.GetString(output.ToArray()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FormattingPreservesExistingNewlineBehavior(bool segmented)
    {
        var random = new Random(42);
        for (var iteration = 0; iteration < 256; iteration++)
        {
            var characters = new char[random.Next(0, 128)];
            for (var i = 0; i < characters.Length; i++)
            {
                characters[i] = "x\r\n\u00e9"[random.Next(4)];
            }

            var text = new string(characters);
            var bytes = Encoding.UTF8.GetBytes(text);
            var payload = segmented
                ? ReadOnlySequenceFactory.SegmentPerByteFactory.CreateWithContent(bytes)
                : new ReadOnlySequence<byte>(bytes);
            using var output = new MemoryStream();
            await ServerSentEventsMessageFormatter.WriteMessageAsync(payload, output, default).DefaultTimeout();

            var lines = text.Split('\n');
            var expected = new StringBuilder();
            if (text.Length > 0)
            {
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (i < lines.Length - 1 && line.Length > 1 && line.EndsWith('\r'))
                    {
                        line = line[..^1];
                    }
                    expected.Append("data: ").Append(line).Append("\r\n");
                }
            }
            expected.Append("\r\n");
            Assert.Equal(expected.ToString(), Encoding.UTF8.GetString(output.ToArray()));
        }
    }

    [Theory]
    [InlineData(32)]
    [InlineData(4096)]
    public async Task MultilineMessageUsesOneOutputWrite(int lineLength)
    {
        var line = new string('x', lineLength);
        var lines = Enumerable.Repeat(line, 16);
        var payload = string.Join("\r\n", lines);
        var expected = string.Concat(lines.Select(value => $"data: {value}\r\n")) + "\r\n";
        var buffer = ReadOnlySequenceFactory.CreateSegments(
            Encoding.UTF8.GetBytes(payload[..(lineLength + 1)]),
            Encoding.UTF8.GetBytes(payload[(lineLength + 1)..]));
        var output = CreateOutputStream((bytes, token) =>
        {
            Assert.Equal(expected, Encoding.UTF8.GetString(bytes.Span));
            return Task.CompletedTask;
        });

        await ServerSentEventsMessageFormatter.WriteMessageAsync(buffer, output.Object, default).DefaultTimeout();

        Assert.Single(output.Invocations);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(128, false)]
    [InlineData(128, true)]
    public async Task WritesOnlyFormattedBytesWhenCarriageReturnsAreTrimmed(int lineCount, bool segmented)
    {
        var text = string.Concat(Enumerable.Repeat("x\r\n", lineCount));
        var expected = string.Concat(Enumerable.Repeat("data: x\r\n", lineCount)) + "data: \r\n\r\n";
        var bytes = Encoding.UTF8.GetBytes(text);
        var payload = segmented
            ? ReadOnlySequenceFactory.SegmentPerByteFactory.CreateWithContent(bytes)
            : new ReadOnlySequence<byte>(bytes);
        var output = CreateOutputStream((written, token) =>
        {
            Assert.Equal(expected.Length, written.Length);
            Assert.Equal(expected, Encoding.UTF8.GetString(written.Span));
            return Task.CompletedTask;
        });

        await ServerSentEventsMessageFormatter.WriteMessageAsync(payload, output.Object, default).DefaultTimeout();

        Assert.Single(output.Invocations);
    }

    [Theory]
    [InlineData(1, 256)]
    [InlineData(8190, 65536)]
    public async Task MultilineMessageDoesNotOverReserveOutputBuffer(int newlineCount, int maximumCapacity)
    {
        var payload = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(new string('\n', newlineCount)));
        var output = CreateOutputStream((bytes, token) =>
        {
            Assert.Equal((8 * newlineCount) + 10, bytes.Length);
            Assert.True(MemoryMarshal.TryGetArray(bytes, out var array));
            Assert.InRange(array.Array.Length, bytes.Length, maximumCapacity);
            return Task.CompletedTask;
        });

        await ServerSentEventsMessageFormatter.WriteMessageAsync(payload, output.Object, default).DefaultTimeout();
        Assert.Single(output.Invocations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FormattingBufferIsRetainedUntilOutputWriteCompletes(bool cancelWrite)
    {
        using var cts = new CancellationTokenSource();
        var pendingWrite = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ReadOnlyMemory<byte> pendingBytes = default;
        var output = CreateOutputStream((bytes, token) =>
        {
            Assert.Equal(cts.Token, token);
            pendingBytes = bytes;
            return pendingWrite.Task;
        });

        var first = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("first\r\nmessage"));
        var writeTask = ServerSentEventsMessageFormatter.WriteMessageAsync(first, output.Object, cts.Token);
        try
        {
            Assert.False(writeTask.IsCompleted);
            var second = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("other\ncontent"));
            await ServerSentEventsMessageFormatter.WriteMessageAsync(second, Stream.Null, default).DefaultTimeout();
            Assert.Equal("data: first\r\ndata: message\r\n\r\n", Encoding.UTF8.GetString(pendingBytes.Span));
        }
        finally
        {
            if (cancelWrite)
            {
                cts.Cancel();
                pendingWrite.TrySetCanceled(cts.Token);
            }
            else
            {
                pendingWrite.TrySetResult();
            }
        }

        if (cancelWrite)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => writeTask).DefaultTimeout();
        }
        else
        {
            await writeTask.DefaultTimeout();
        }
    }

    [Fact]
    public async Task FailedOutputWriteDoesNotAffectNextMessage()
    {
        var error = new IOException("Write failed.");
        var output = CreateOutputStream((bytes, token) => Task.FromException(error));
        var payload = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("first\nmessage"));

        var actual = await Assert.ThrowsAsync<IOException>(
            () => ServerSentEventsMessageFormatter.WriteMessageAsync(payload, output.Object, default)).DefaultTimeout();
        Assert.Same(error, actual);

        using var nextOutput = new MemoryStream();
        await ServerSentEventsMessageFormatter.WriteMessageAsync(
            new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("next\nmessage")), nextOutput, default).DefaultTimeout();
        Assert.Equal("data: next\r\ndata: message\r\n\r\n", Encoding.UTF8.GetString(nextOutput.ToArray()));
    }

    [Fact]
    public async Task CanceledMultilineMessageDoesNotWriteOutput()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var output = new Mock<Stream>(MockBehavior.Strict);
        var payload = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("first\nmessage"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ServerSentEventsMessageFormatter.WriteMessageAsync(payload, output.Object, cts.Token)).DefaultTimeout();

        output.VerifyNoOtherCalls();
    }

    private static Mock<Stream> CreateOutputStream(Func<ReadOnlyMemory<byte>, CancellationToken, Task> writeAsync)
    {
        var output = new Mock<Stream>(MockBehavior.Strict);
        output.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((byte[] bytes, int offset, int count, CancellationToken token) => writeAsync(bytes.AsMemory(offset, count), token));
        output.Setup(s => s.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns((ReadOnlyMemory<byte> bytes, CancellationToken token) => new ValueTask(writeAsync(bytes, token)));

        return output;
    }

    public static IEnumerable<object[]> PayloadData => new List<object[]>
        {
            new object[] { "\r\n", "" },
            new object[] { "data: Hello, World\r\n\r\n", "Hello, World" },
            new object[] { "data: Hello\r\ndata: World\r\n\r\n", "Hello\r\nWorld" },
            new object[] { "data: Hello\r\ndata: World\r\n\r\n", "Hello\nWorld" },
            new object[] { "data: Hello\r\ndata: \r\n\r\n", "Hello\n" },
            new object[] { "data: Hello\r\ndata: \r\n\r\n", "Hello\r\n" },
            new object[] { "data: \r\ndata: \r\n\r\n", "\n" },
            new object[] { "data: \r\ndata: \r\ndata: \r\n\r\n", "\n\n" },
            new object[] { "data: \u00e9\r\ndata: \u03bb\r\n\r\n", "\u00e9\n\u03bb" },
        };
}

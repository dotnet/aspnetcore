// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.IO;
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

    [Fact]
    public async Task MultilineMessageWritesOnlyFormattedBytesInOneWrite()
    {
        const string expected = "data: first\r\ndata: second\r\ndata: \r\n\r\n";
        var buffer = ReadOnlySequenceFactory.CreateSegments(
            Encoding.UTF8.GetBytes("first\r"),
            Encoding.UTF8.GetBytes("\nsecond\r\n"));
        var output = CreateOutputStream((bytes, token) =>
        {
            Assert.Equal(expected.Length, bytes.Length);
            Assert.Equal(expected, Encoding.UTF8.GetString(bytes.Span));
            return Task.CompletedTask;
        });

        await ServerSentEventsMessageFormatter.WriteMessageAsync(buffer, output.Object, default).DefaultTimeout();

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
            new object[] { "data: \r\r\ndata: \r\n\r\n", "\r\n" },
            new object[] { "data: Hello\rWorld\r\ndata: Next\r\ndata: Last\r\r\n\r\n", "Hello\rWorld\r\nNext\nLast\r" },
            new object[] { "data: \u00e9\r\ndata: \u03bb\r\n\r\n", "\u00e9\r\n\u03bb" },
        };
}

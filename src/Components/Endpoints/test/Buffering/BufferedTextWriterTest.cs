// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

public class BufferedTextWriterTest
{
    [Fact]
    public void DoesNotSupportFlushSync()
    {
        var writer = new BufferedTextWriter(new StringWriter());
        Assert.Throws<NotSupportedException>(writer.Flush);
    }

    [Fact]
    public async Task WritesThroughToUnderlyingWriter()
    {
        var underlying = new StringWriter();
        var writer = new BufferedTextWriter(underlying);

        writer.Write('x');
        writer.Write("My string");
        writer.Write(new char[] { 'a', 'b', 'c', 'd', 'e' }, 1, 3);

        Assert.Empty(underlying.ToString());
        await writer.FlushAsync();

        Assert.Equal("xMy stringbcd", underlying.ToString());
    }

    [Fact]
    public async Task CanWriteAndFlushWhileFlushIsInProgress()
    {
        var underlying = new SlowFlushingTextWriter();
        var writer = new BufferedTextWriter(underlying);

        // Start the first flush
        writer.Write('a');
        var flush1Tcs = new TaskCompletionSource();
        underlying.CompleteNextFlushAfter = flush1Tcs.Task;
        var flushAsyncTask1 = writer.FlushAsync();
        Assert.False(flushAsyncTask1.IsCompleted);

        // While that's running, write more then do a second flush
        writer.Write('b');
        var flush2Tcs = new TaskCompletionSource();
        underlying.CompleteNextFlushAfter = flush2Tcs.Task;
        var flushAsyncTask2 = writer.FlushAsync();
        Assert.False(flushAsyncTask2.IsCompleted);

        // Can add more output that will be included in the second flush because it hasn't started yet
        writer.Write('c');

        // Can add a third flush that completes with the second (because the second hasn't started yet)
        var flushAsyncTask3 = writer.FlushAsync();
        Assert.False(flushAsyncTask3.IsCompleted);

        // See the first flush can complete
        Assert.Empty(underlying.FlushedOutput);
        flush1Tcs.SetResult();
        await flushAsyncTask1;
        Assert.Equal("a", underlying.FlushedOutput);
        Assert.False(flushAsyncTask2.IsCompleted);

        // See the second flush can complete, which immediately completes the third since they were coalesced
        flush2Tcs.SetResult();
        await flushAsyncTask2;
        Assert.True(flushAsyncTask3.IsCompleted);
        Assert.Equal("abc", underlying.FlushedOutput);
    }

    [Fact]
    public async Task CanWriteCharSegmentsAcrossMultipleFlushes()
    {
        var underlying = new StringWriter();
        var writer = new BufferedTextWriter(underlying);

        for (var i = 0; i < 5; i++)
        {
            var chars = $"[{i}]".ToCharArray();
            writer.Write(chars, 0, chars.Length);
            writer.Write($"<{i}>".AsSpan());
            writer.Write('|');
            await writer.FlushAsync();
        }

        Assert.Equal("[0]<0>|[1]<1>|[2]<2>|[3]<3>|[4]<4>|", underlying.ToString());
    }

    [Fact]
    public async Task CanWriteCharSegmentsWhileFlushIsInProgress()
    {
        var underlying = new SlowFlushingTextWriter();
        var writer = new BufferedTextWriter(underlying);

        // Start the first flush
        writer.Write("ab".AsSpan());
        var flush1Tcs = new TaskCompletionSource();
        underlying.CompleteNextFlushAfter = flush1Tcs.Task;
        var flushAsyncTask1 = writer.FlushAsync();
        Assert.False(flushAsyncTask1.IsCompleted);

        // While that's running, write more segments and start a second flush
        writer.Write(new[] { 'c', 'd' }, 0, 2);
        underlying.CompleteNextFlushAfter = null;
        var flushAsyncTask2 = writer.FlushAsync();

        flush1Tcs.SetResult();
        await flushAsyncTask1;
        await flushAsyncTask2;
        Assert.Equal("abcd", underlying.FlushedOutput);

        // Buffers that were flushed can be reused for further segments
        writer.Write("ef".AsSpan());
        await writer.FlushAsync();
        writer.Write("gh".AsSpan());
        await writer.FlushAsync();
        Assert.Equal("abcdefgh", underlying.FlushedOutput);
    }

    private class SlowFlushingTextWriter : StringWriter
    {
        public Task CompleteNextFlushAfter { get; set; }

        public string FlushedOutput { get; private set; } = string.Empty;

        public override async Task FlushAsync()
        {
            if (CompleteNextFlushAfter is not null)
            {
                await CompleteNextFlushAfter;
            }

            await base.FlushAsync();
            FlushedOutput = ToString();
        }
    }
}

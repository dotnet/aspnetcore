// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.ResponseCaching.Tests;

public class CachedResponseBodyTests
{
    [Fact]
    public void GetSegments()
    {
        var segments = new List<byte[]>();
        var body = new CachedResponseBody(segments, 0);

        Assert.Same(segments, body.Segments);
    }

    [Fact]
    public void GetLength()
    {
        var segments = new List<byte[]>();
        var body = new CachedResponseBody(segments, 42);

        Assert.Equal(42, body.Length);
    }

    [Fact]
    public async Task Copy_DoNothingWhenNoSegments()
    {
        var segments = new List<byte[]>();
        var receivedSegments = new List<byte[]>();
        var body = new CachedResponseBody(segments, 0);

        var pipe = new Pipe();

        var receiverTask = ReceiveDataAsync(pipe.Reader, receivedSegments);
        var copyTask = CopyDataAsync(body, pipe.Writer);

        await Task.WhenAll(receiverTask, copyTask).DefaultTimeout();

        Assert.Empty(receivedSegments);
    }

    [Fact]
    public async Task Copy_SingleSegment()
    {
        var segments = new List<byte[]>
            {
                new byte[] { 1 }
            };
        var receivedSegments = new List<byte[]>();
        var body = new CachedResponseBody(segments, 0);

        var pipe = new Pipe();

        var receiverTask = ReceiveDataAsync(pipe.Reader, receivedSegments);
        var copyTask = CopyDataAsync(body, pipe.Writer);

        await Task.WhenAll(receiverTask, copyTask).DefaultTimeout();

        Assert.Equal(segments, receivedSegments);
    }

    [Fact]
    public async Task Copy_MultipleSegments()
    {
        var segments = new List<byte[]>
            {
                new byte[] { 1 },
                new byte[] { 2, 3 }
            };
        var receivedSegments = new List<byte[]>();
        var body = new CachedResponseBody(segments, 0);

        var pipe = new Pipe();

        var receiverTask = ReceiveDataAsync(pipe.Reader, receivedSegments);
        var copyTask = CopyDataAsync(body, pipe.Writer);

        await Task.WhenAll(receiverTask, copyTask).DefaultTimeout();

        Assert.Equal(new byte[] { 1, 2, 3 }, receivedSegments.SelectMany(x => x).ToArray());
    }

    static async Task CopyDataAsync(CachedResponseBody body, PipeWriter writer)
    {
        try
        {
            await body.CopyToAsync(writer, CancellationToken.None);
        }
        finally
        {
            await writer.CompleteAsync();
        }
    }

    static async Task ReceiveDataAsync(PipeReader reader, List<byte[]> receivedSegments)
    {
        while (true)
        {
            var result = await reader.ReadAsync();
            var buffer = result.Buffer;

            foreach (var memory in buffer)
            {
                receivedSegments.Add(memory.ToArray());
            }

            reader.AdvanceTo(buffer.End, buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }
        await reader.CompleteAsync();
    }
}

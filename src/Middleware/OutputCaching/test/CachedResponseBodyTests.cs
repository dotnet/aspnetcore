// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.OutputCaching.Tests;

public class CachedResponseBodyTests
{
    private readonly int _timeout = Debugger.IsAttached ? -1 : 5000;

    [Fact]
    public void GetSegments()
    {
        var segments = new List<byte[]>();
        var body = RecyclableReadOnlySequenceSegment.CreateSequence(segments);

        Assert.True(body.IsEmpty);
        RecyclableReadOnlySequenceSegment.RecycleChain(body);
    }

    [Fact]
    public async Task Copy_DoNothingWhenNoSegments()
    {
        var segments = new List<byte[]>();
        var receivedSegments = new List<byte[]>();
        var body = RecyclableReadOnlySequenceSegment.CreateSequence(segments);

        var pipe = new Pipe();
        using var cts = new CancellationTokenSource(_timeout);

        var receiverTask = ReceiveDataAsync(pipe.Reader, receivedSegments, cts.Token);
        var copyTask = RecyclableReadOnlySequenceSegment.CopyToAsync(body, pipe.Writer, cts.Token).AsTask().ContinueWith(t => pipe.Writer.CompleteAsync(t.Exception));

        await Task.WhenAll(receiverTask, copyTask);

        Assert.Empty(receivedSegments);
        RecyclableReadOnlySequenceSegment.RecycleChain(body);
    }

    [Fact]
    public async Task Copy_SingleSegment()
    {
        var segments = new List<byte[]>
            {
                new byte[] { 1 }
            };
        var receivedSegments = new List<byte[]>();
        var body = RecyclableReadOnlySequenceSegment.CreateSequence(segments);

        var pipe = new Pipe();

        using var cts = new CancellationTokenSource(_timeout);

        var receiverTask = ReceiveDataAsync(pipe.Reader, receivedSegments, cts.Token);
        var copyTask = CopyDataAsync(body, pipe.Writer, cts.Token);

        await Task.WhenAll(receiverTask, copyTask);

        Assert.Equal(segments, receivedSegments);
        RecyclableReadOnlySequenceSegment.RecycleChain(body);
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
        var body = RecyclableReadOnlySequenceSegment.CreateSequence(segments);

        var pipe = new Pipe();

        using var cts = new CancellationTokenSource(_timeout);

        var receiverTask = ReceiveDataAsync(pipe.Reader, receivedSegments, cts.Token);
        var copyTask = CopyDataAsync(body, pipe.Writer, cts.Token);

        await Task.WhenAll(receiverTask, copyTask);

        Assert.Equal(new byte[] { 1, 2, 3 }, receivedSegments.SelectMany(x => x).ToArray());
        RecyclableReadOnlySequenceSegment.RecycleChain(body);
    }

    static async Task CopyDataAsync(ReadOnlySequence<byte> body, PipeWriter writer, CancellationToken cancellationToken)
    {
        await RecyclableReadOnlySequenceSegment.CopyToAsync(body, writer, cancellationToken);
        await writer.CompleteAsync();
    }

    static async Task ReceiveDataAsync(PipeReader reader, List<byte[]> receivedSegments, CancellationToken cancellationToken)
    {
        while (true)
        {
            var result = await reader.ReadAsync(cancellationToken);
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

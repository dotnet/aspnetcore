// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol;

public class AckPipeTests
{
    private const int FrameSize = 24;

    [Fact]
    public async Task CanSendAndReceiveTransport()
    {
        var duplexPipe = CreateConnectionPair(new PipeOptions(), new PipeOptions());

        var values = new byte[] { 1, 2, 3, 4, 5 };
        var flushRes = await duplexPipe.Transport.Output.WriteAsync(values);

        Assert.False(flushRes.IsCanceled);
        Assert.False(flushRes.IsCompleted);

        var readResult = await duplexPipe.Application.Input.ReadAsync();

        Assert.False(readResult.IsCanceled);
        Assert.False(readResult.IsCompleted);
        Assert.Equal(values.Length, readResult.Buffer.Length);
        Assert.Equal(values, readResult.Buffer.ToArray());
    }

    [Fact]
    public async Task CanSendAndReceiveLargeAmount()
    {
        var duplexPipe = CreateConnectionPair(new PipeOptions(), new PipeOptions());

        var values = new byte[20000];
        Random.Shared.NextBytes(values);
        var flushRes = await duplexPipe.Transport.Output.WriteAsync(values);

        Assert.False(flushRes.IsCanceled);
        Assert.False(flushRes.IsCompleted);

        var readResult = await duplexPipe.Application.Input.ReadAsync();

        Assert.False(readResult.IsCanceled);
        Assert.False(readResult.IsCompleted);
        Assert.Equal(values.Length, readResult.Buffer.Length);
        Assert.Equal(values, readResult.Buffer.ToArray());
    }

    [Fact]
    public async Task CanSendAndReceiveLargeAmount_ManyWritesSingleFlush()
    {
        var duplexPipe = CreateConnectionPair(new PipeOptions(), new PipeOptions());

        var values = new byte[20000];
        Random.Shared.NextBytes(values);
        var written = 0;
        while (written < values.Length)
        {
            var mem = duplexPipe.Transport.Output.GetMemory();
            var toWrite = Math.Min(mem.Length, values.Length - written);
            values.AsSpan(written, toWrite).CopyTo(mem.Span);
            duplexPipe.Transport.Output.Advance(toWrite);
            written += toWrite;
        }

        var flushRes = await duplexPipe.Transport.Output.FlushAsync();

        Assert.False(flushRes.IsCanceled);
        Assert.False(flushRes.IsCompleted);

        var readResult = await duplexPipe.Application.Input.ReadAsync();

        Assert.False(readResult.IsCanceled);
        Assert.False(readResult.IsCompleted);
        Assert.Equal(values.Length, readResult.Buffer.Length);
        Assert.Equal(values, readResult.Buffer.ToArray());
    }

    [Fact]
    public async Task ReadFromTransportRemovesFraming()
    {
        var duplexPipe = CreateClient();

        var buffer = new byte[28];
        Random.Shared.NextBytes(buffer);
        WriteFrame(buffer, buffer.Length - FrameSize, 0);

        // "write" from server
        await duplexPipe.Application.Output.WriteAsync(buffer);

        // read in client application layer
        var res = await duplexPipe.Transport.Input.ReadAsync();
        Assert.Equal(4, res.Buffer.Length);
        Assert.Equal(buffer.AsSpan(FrameSize).ToArray(), res.Buffer.ToArray());
    }

    [Fact]
    public async Task WriteFromApplicationAddsFraming()
    {
        var duplexPipe = CreateClient();

        var buffer = new byte[20];
        Random.Shared.NextBytes(buffer);

        await duplexPipe.Transport.Output.WriteAsync(buffer);

        var res = await duplexPipe.Application.Input.ReadAsync();
        var framing = ReadFrame(res.Buffer.ToArray());

        Assert.Equal(buffer.Length, framing.Length);
        Assert.Equal(0, framing.AckId);
        Assert.Equal(buffer.Length + FrameSize, res.Buffer.Length);
        Assert.Equal(buffer, res.Buffer.Slice(FrameSize).ToArray());
    }

    [Fact]
    public async Task MultipleWritesSingleFlushFromApplicationAddsFraming()
    {
        var duplexPipe = CreateClient();

        var buffer = new byte[20];
        Random.Shared.NextBytes(buffer);

        for (var i = 0; i < 3; i++)
        {
            var memory = duplexPipe.Transport.Output.GetMemory();
            buffer.CopyTo(memory);
            duplexPipe.Transport.Output.Advance(buffer.Length);
        }
        await duplexPipe.Transport.Output.FlushAsync();

        var res = await duplexPipe.Application.Input.ReadAsync();
        var framing = ReadFrame(res.Buffer.ToArray());

        Assert.Equal(buffer.Length * 3, framing.Length);
        Assert.Equal(0, framing.AckId);
        Assert.Equal(buffer.Length * 3 + FrameSize, res.Buffer.Length);
        Assert.Equal(buffer, res.Buffer.Slice(FrameSize, buffer.Length).ToArray());
        Assert.Equal(buffer, res.Buffer.Slice(FrameSize + buffer.Length, buffer.Length).ToArray());
        Assert.Equal(buffer, res.Buffer.Slice(FrameSize + buffer.Length * 2, buffer.Length).ToArray());
    }

    [Fact]
    public async Task ReadFromTransportAcrossMultipleReads()
    {
        var duplexPipe = CreateClient();

        var buffer = new byte[28];
        Random.Shared.NextBytes(buffer);
        WriteFrame(buffer, buffer.Length - FrameSize + buffer.Length + buffer.Length, 0);

        // "write" from server
        await duplexPipe.Application.Output.WriteAsync(buffer);

        // read in client application layer
        var res = await duplexPipe.Transport.Input.ReadAsync();

        Assert.Equal(4, res.Buffer.Length);
        Assert.Equal(buffer.AsSpan(FrameSize).ToArray(), res.Buffer.ToArray());

        // consume nothing
        duplexPipe.Transport.Input.AdvanceTo(res.Buffer.Start, res.Buffer.End);

        await duplexPipe.Application.Output.WriteAsync(buffer);
        res = await duplexPipe.Transport.Input.ReadAsync();

        Assert.Equal(32, res.Buffer.Length);

        // consume nothing
        duplexPipe.Transport.Input.AdvanceTo(res.Buffer.Start, res.Buffer.End);

        await duplexPipe.Application.Output.WriteAsync(buffer);
        res = await duplexPipe.Transport.Input.ReadAsync();

        Assert.Equal(60, res.Buffer.Length);

        // consume everything
        duplexPipe.Transport.Input.AdvanceTo(res.Buffer.End);

        // New write to make sure internal state is cleared from completed read
        WriteFrame(buffer, buffer.Length - FrameSize, 0);
        await duplexPipe.Application.Output.WriteAsync(buffer);
        res = await duplexPipe.Transport.Input.ReadAsync();

        Assert.Equal(4, res.Buffer.Length);
    }

    [Fact]
    public async Task ManyWritesSingleFlush_WritesSingleFrame()
    {
        var duplexPipe = CreateClient();

        var buffer = new byte[20];
        Random.Shared.NextBytes(buffer);

        var memory = duplexPipe.Transport.Output.GetMemory();
        Assert.True(memory.Length > buffer.Length);
        buffer.CopyTo(memory);
        duplexPipe.Transport.Output.Advance(buffer.Length);

        memory = duplexPipe.Transport.Output.GetMemory();
        Assert.True(memory.Length > buffer.Length);
        buffer.CopyTo(memory);
        duplexPipe.Transport.Output.Advance(buffer.Length);

        memory = duplexPipe.Transport.Output.GetMemory();
        Assert.True(memory.Length > buffer.Length);
        buffer.CopyTo(memory);
        duplexPipe.Transport.Output.Advance(buffer.Length);

        await duplexPipe.Transport.Output.FlushAsync();

        var res = await duplexPipe.Application.Input.ReadAsync();
        var framing = ReadFrame(res.Buffer.ToArray());

        Assert.Equal(buffer.Length * 3, framing.Length);
        Assert.Equal(0, framing.AckId);
        Assert.Equal(framing.Length + FrameSize, res.Buffer.Length);

        var buf = res.Buffer.Slice(FrameSize);
        while (buf.Length > 0)
        {
            Assert.Equal(buffer, buf.Slice(0, buffer.Length).ToArray());
            buf = buf.Slice(buffer.Length);
        }
    }

    [Fact(Skip = "Something we want to support?")]
    public async Task ReadFromTransportAcrossFrames()
    {
        var duplexPipe = CreateClient();

        var buffer = new byte[20];
        Random.Shared.NextBytes(buffer);
        WriteFrame(buffer, buffer.Length - FrameSize, 0);

        // "write" from server
        await duplexPipe.Application.Output.WriteAsync(buffer);
        await duplexPipe.Application.Output.WriteAsync(buffer);

        // read in client application layer
        var res = await duplexPipe.Transport.Input.ReadAsync();

        Assert.Equal(4, res.Buffer.Length);
        Assert.Equal(buffer.AsSpan(FrameSize).ToArray(), res.Buffer.ToArray());

        // consume nothing
        duplexPipe.Transport.Input.AdvanceTo(res.Buffer.Start, res.Buffer.End);

        res = await duplexPipe.Transport.Input.ReadAsync();
        // ??
    }

    [Fact]
    public async Task AckFromTransportReadUpdatesApplicationBuffer()
    {
        var duplexPipe = CreateClient();
        // write something so we can ack it and see that the pipe has nothing in it
        await duplexPipe.Transport.Output.WriteAsync(new byte[2]);

        var res = await duplexPipe.Application.Input.ReadAsync();
        // in real usage this will be advanced properly
        // but we're claiming we read nothing so we can observe the ack behavior in the next read 
        duplexPipe.Application.Input.AdvanceTo(res.Buffer.Start);

        var buffer = new byte[28];
        Random.Shared.NextBytes(buffer);
        WriteFrame(buffer, buffer.Length - 24, ackId: FrameSize + 2);

        // "write" from server
        await duplexPipe.Application.Output.WriteAsync(buffer);

        // read in client application layer
        // this reads the ack from the "server" and updates state
        _ = await duplexPipe.Transport.Input.ReadAsync();

        // this will be an empty read because the ack will be applied and everything will be marked as read
        res = await duplexPipe.Application.Input.ReadAsync();

        Assert.Equal(0, res.Buffer.Length);
        Assert.False(res.IsCanceled);
        Assert.False(res.IsCompleted);
    }

    [Fact]
    public async Task AckFromTransportReadUpdatesApplicationBuffer_CanReadNewDataAfter()
    {
        // Basically the same test as AckFromTransportReadUpdatesApplicationBuffer but we write more data after the ack has fully flowed
        // Just to smoke test that the pipe is still usable

        var duplexPipe = CreateClient();
        // write something so we can ack it
        await duplexPipe.Transport.Output.WriteAsync(new byte[2]);

        var res = await duplexPipe.Application.Input.ReadAsync();
        // in real usage this will be advanced properly
        // but we're claiming we read nothing so we can observe the ack behavior in the next read
        duplexPipe.Application.Input.AdvanceTo(res.Buffer.Start);

        var buffer = new byte[FrameSize + 4];
        Random.Shared.NextBytes(buffer);
        WriteFrame(buffer, buffer.Length - FrameSize, ackId: FrameSize + 2);

        // "write" from server
        await duplexPipe.Application.Output.WriteAsync(buffer);

        // read in client application layer
        // this reads the ack from the "server" and updates state
        res = await duplexPipe.Transport.Input.ReadAsync();
        duplexPipe.Transport.Input.AdvanceTo(res.Buffer.End);

        // write again to update total sent
        await duplexPipe.Transport.Output.WriteAsync(new byte[2] { 42, 99 });

        res = await duplexPipe.Application.Input.ReadAsync();

        Assert.Equal(FrameSize + 2, res.Buffer.Length);
        var (len, ack) = ReadFrame(res.Buffer.ToArray());
        Assert.Equal(2, len);
        Assert.Equal(FrameSize + 4, ack);
        Assert.Equal(new byte[] { 42, 99 }, res.Buffer.Slice(FrameSize).ToArray());
        Assert.False(res.IsCanceled);
        Assert.False(res.IsCompleted);
    }

    [Fact]
    public async Task ReceiveAckIdLargerThanTotalSentErrors()
    {
        var duplexPipe = CreateClient();

        var buffer = new byte[28];
        Random.Shared.NextBytes(buffer);
        // ackId more than what has been sent
        WriteFrame(buffer, buffer.Length - FrameSize, ackId: 30);

        // "write" from server
        await duplexPipe.Application.Output.WriteAsync(buffer);

        // read in client application layer
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await duplexPipe.Transport.Input.ReadAsync());
        Assert.Equal("Ack ID '30' is greater than total amount of '0' bytes that have been sent.", exception.Message);

        // Pipe is completed
        exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await duplexPipe.Transport.Input.ReadAsync());
        Assert.Equal("Reading is not allowed after reader was completed.", exception.Message);
    }

    // This is a fun edge case test, where if we have consumed everything in a BufferSegment and Acked everything too
    // then consumed points to the end of the Segment, while Ack points to the beginning of the next Segment
    // This test verifies that everything behaves correctly in that case
    [Fact]
    public async Task ConsumeAndAckAtEndOfSegment_CanServeNextSegment()
    {
        var duplexPipe = CreateClient();

        var buffer = new byte[4072];
        Random.Shared.NextBytes(buffer);

        // "write" from server
        await duplexPipe.Transport.Output.WriteAsync(buffer);

        // read in client application layer
        var res = await duplexPipe.Application.Input.ReadAsync();
        duplexPipe.Application.Input.AdvanceTo(res.Buffer.End);

        Random.Shared.NextBytes(buffer);
        await duplexPipe.Transport.Output.WriteAsync(buffer);

        var appBuffer = new byte[28];
        Random.Shared.NextBytes(appBuffer);
        WriteFrame(appBuffer, appBuffer.Length - FrameSize, 4096);
        await duplexPipe.Application.Output.WriteAsync(appBuffer);

        // Updates Ack in Application.Input
        await duplexPipe.Transport.Input.ReadAsync();

        res = await duplexPipe.Application.Input.ReadAsync();
        Assert.Equal(4096, res.Buffer.Length);
        var (len, ack) = ReadFrame(res.Buffer.ToArray());
        Assert.Equal(4072, len);
        Assert.Equal(0, ack);
        Assert.Equal(buffer, res.Buffer.Slice(FrameSize).ToArray());
        Assert.True(res.Buffer.IsSingleSegment);
    }

    [Fact]
    public async Task ApplicationSendsAck()
    {
        var duplexPipe = CreateClient();

        var buffer = new byte[FrameSize + 4];
        Random.Shared.NextBytes(buffer);
        WriteFrame(buffer, buffer.Length - FrameSize, 0);

        // "write" from server
        await duplexPipe.Application.Output.WriteAsync(buffer);

        // read in client application layer
        var res = await duplexPipe.Transport.Input.ReadAsync();
        duplexPipe.Transport.Input.AdvanceTo(res.Buffer.End);

        _ = await duplexPipe.Transport.Output.WriteAsync(new byte[2]);

        res = await duplexPipe.Application.Input.ReadAsync();
        var (length, ackId) = ReadFrame(res.Buffer.ToArray());

        Assert.Equal(2, length);
        Assert.Equal(FrameSize + 4, ackId);
    }

    [Fact]
    public async Task ApplicationSendsAckWithMultiSegment_ConsumingWhileReading()
    {
        var duplexPipe = CreateClient();

        var buffer = new byte[FrameSize + 5];
        Random.Shared.NextBytes(buffer);
        WriteFrame(buffer, buffer.Length - FrameSize, 0);

        // "write" from server, 26 of the 29 bytes, we want to force the reader to do two reads to get the full data
        await duplexPipe.Application.Output.WriteAsync(buffer.AsSpan(0, FrameSize + 2).ToArray());

        // read in client application layer
        var res = await duplexPipe.Transport.Input.ReadAsync();
        Assert.Equal(2, res.Buffer.Length);
        Assert.Equal(buffer.AsSpan(FrameSize, 2).ToArray(), res.Buffer.ToArray());
        // Consume all seen so far
        duplexPipe.Transport.Input.AdvanceTo(res.Buffer.End);

        // write again, the last 3 of the 29 bytes
        await duplexPipe.Application.Output.WriteAsync(buffer.AsSpan(FrameSize + 2, 3).ToArray());

        res = await duplexPipe.Transport.Input.ReadAsync();
        Assert.Equal(3, res.Buffer.Length);
        Assert.Equal(buffer.AsSpan(FrameSize + 2, 3).ToArray(), res.Buffer.ToArray());
        duplexPipe.Transport.Input.AdvanceTo(res.Buffer.End);

        _ = await duplexPipe.Transport.Output.WriteAsync(new byte[2]);

        res = await duplexPipe.Application.Input.ReadAsync();
        var (length, ackId) = ReadFrame(res.Buffer.ToArray());

        Assert.Equal(2, length);
        Assert.Equal(FrameSize + 5, ackId);
    }

    [Fact]
    public async Task ApplicationSendsAckWithMultiSegment_OnlyConsumeAtEnd()
    {
        var duplexPipe = CreateClient();

        var buffer = new byte[29];
        Random.Shared.NextBytes(buffer);
        WriteFrame(buffer, buffer.Length - FrameSize, 0);

        // "write" from server, 26 of the 29 bytes, we want to force the reader to do two reads to get the full data
        await duplexPipe.Application.Output.WriteAsync(buffer.AsSpan(0, 26).ToArray());

        // read in client application layer
        var res = await duplexPipe.Transport.Input.ReadAsync();
        Assert.Equal(2, res.Buffer.Length);
        Assert.Equal(buffer.AsSpan(FrameSize, 2).ToArray(), res.Buffer.ToArray());
        // Don't consume any
        duplexPipe.Transport.Input.AdvanceTo(res.Buffer.Start, res.Buffer.End);

        // write again, the last 3 of the 29 bytes
        await duplexPipe.Application.Output.WriteAsync(buffer.AsSpan(26, 3).ToArray());

        res = await duplexPipe.Transport.Input.ReadAsync();
        Assert.Equal(5, res.Buffer.Length);
        Assert.Equal(buffer.AsSpan(FrameSize, 5).ToArray(), res.Buffer.ToArray());
        duplexPipe.Transport.Input.AdvanceTo(res.Buffer.End);

        _ = await duplexPipe.Transport.Output.WriteAsync(new byte[2]);

        res = await duplexPipe.Application.Input.ReadAsync();
        var (length, ackId) = ReadFrame(res.Buffer.ToArray());

        Assert.Equal(2, length);
        Assert.Equal(29, ackId);
    }

    [Fact]
    public async Task CompleteWithErrorFromTransportWriterFlowsToAppReader()
    {
        var duplexPipe = CreateClient();

        duplexPipe.Transport.Output.Complete(new Exception("custom"));

        var ex = await Assert.ThrowsAsync<Exception>(async () => await duplexPipe.Application.Input.ReadAsync());
        Assert.Equal("custom", ex.Message);
    }

    [Fact]
    public async Task CompleteWithErrorFromTransportReaderFlowsToAppWriter()
    {
        var duplexPipe = CreateClient();

        duplexPipe.Transport.Input.Complete(new Exception("custom"));

        var ex = await Assert.ThrowsAsync<Exception>(async () => await duplexPipe.Application.Output.FlushAsync());
        Assert.Equal("custom", ex.Message);
    }

    [Fact]
    public async Task CompleteWithErrorFromAppWriterFlowsToTransportReader()
    {
        var duplexPipe = CreateClient();

        duplexPipe.Application.Output.Complete(new Exception("custom"));

        var ex = await Assert.ThrowsAsync<Exception>(async () => await duplexPipe.Transport.Input.ReadAsync());
        Assert.Equal("custom", ex.Message);
    }

    [Fact]
    public async Task CompleteWithErrorFromAppReaderFlowsToTransportWriter()
    {
        var duplexPipe = CreateClient();

        duplexPipe.Application.Input.Complete(new Exception("custom"));

        var ex = await Assert.ThrowsAsync<Exception>(async () => await duplexPipe.Transport.Output.WriteAsync(new byte[1]));
        Assert.Equal("custom", ex.Message);
    }

    [Fact]
    public async Task TriggerResendWithNothingWritten()
    {
        var duplexPipe = CreateClient();

        var reader = (AckPipeReader)duplexPipe.Application.Input;
        reader.Resend();

        await duplexPipe.Transport.Output.WriteAsync(new byte[2]);

        var res = await duplexPipe.Application.Input.ReadAsync();

        Assert.Equal(FrameSize + 2, res.Buffer.Length);
        Assert.False(res.IsCanceled);
        Assert.False(res.IsCompleted);

        var (length, ackId) = ReadFrame(res.Buffer.ToArray());
        Assert.Equal(2, length);
        Assert.Equal(0, ackId);
    }

    [Fact]
    public async Task TriggerResendWithEverythingAcked()
    {
        var duplexPipe = CreateClient();

        await duplexPipe.Transport.Output.WriteAsync(new byte[2]);
        // Read to pretend we've sent 18 bytes, so that an ack will be allowed
        var res = await duplexPipe.Application.Input.ReadAsync();
        duplexPipe.Application.Input.AdvanceTo(res.Buffer.Start, res.Buffer.Start);

        var buffer = new byte[FrameSize];
        WriteFrame(buffer, 0, FrameSize + 2);
        await duplexPipe.Application.Output.WriteAsync(buffer);

        // Updates ack from App.Output in App.Input
        _ = await duplexPipe.Transport.Input.ReadAsync();

        var reader = (AckPipeReader)duplexPipe.Application.Input;
        reader.Resend();

        // Nothing returned since everything was acked before resend triggered
        res = await duplexPipe.Application.Input.ReadAsync();

        Assert.Equal(0, res.Buffer.Length);
        Assert.True(res.IsCanceled);
        Assert.False(res.IsCompleted);
        duplexPipe.Application.Input.AdvanceTo(res.Buffer.End);

        // smoke testing that we can still receive
        await duplexPipe.Transport.Output.WriteAsync(new byte[2]);

        res = await duplexPipe.Application.Input.ReadAsync();

        Assert.Equal(FrameSize + 2, res.Buffer.Length);
        var (len, ackId) = ReadFrame(res.Buffer.ToArray());
        Assert.Equal(2, len);
        Assert.Equal(0, ackId);
        Assert.False(res.IsCanceled);
        Assert.False(res.IsCompleted);
    }

    [Fact]
    public async Task TriggerResendSendsEverythingNotAcked()
    {
        var duplexPipe = CreateClient();

        // Write two frames of data
        await duplexPipe.Transport.Output.WriteAsync(new byte[2] { 1, 2 });
        await duplexPipe.Transport.Output.WriteAsync(new byte[2] { 3, 4 });

        var reader = (AckPipeReader)duplexPipe.Application.Input;
        reader.Resend();

        var res = await duplexPipe.Application.Input.ReadAsync();

        Assert.Equal(52, res.Buffer.Length);
        Assert.False(res.IsCanceled);
        Assert.False(res.IsCompleted);
        var (len, ackId) = ReadFrame(res.Buffer.ToArray());
        Assert.Equal(2, len);
        Assert.Equal(0, ackId);
        Assert.Equal(new byte[] { 1, 2 }, res.Buffer.ToArray().AsSpan(FrameSize, 2).ToArray());
        (len, ackId) = ReadFrame(res.Buffer.ToArray().AsSpan(FrameSize + 2).ToArray());
        Assert.Equal(2, len);
        Assert.Equal(0, ackId);
        Assert.Equal(new byte[] { 3, 4 }, res.Buffer.ToArray().AsSpan(FrameSize * 2 + 2, 2).ToArray());

        duplexPipe.Application.Input.AdvanceTo(res.Buffer.End);

        // smoke testing that we can still receive
        await duplexPipe.Transport.Output.WriteAsync(new byte[2] { 4, 5 });

        res = await duplexPipe.Application.Input.ReadAsync();

        Assert.Equal(FrameSize + 2, res.Buffer.Length);
        (len, ackId) = ReadFrame(res.Buffer.ToArray());
        Assert.Equal(2, len);
        Assert.Equal(0, ackId);
        Assert.Equal(new byte[] { 4, 5 }, res.Buffer.ToArray().AsSpan(FrameSize, 2).ToArray());
        Assert.False(res.IsCanceled);
        Assert.False(res.IsCompleted);
    }

    [Fact]
    public async Task TriggerResendWhenPartialFrameAcked()
    {
        var duplexPipe = CreateClient();

        await duplexPipe.Transport.Output.WriteAsync(new byte[] { 1, 2, 3, 4, 5, 6, 7 });
        // Read to pretend we've sent 31 bytes, so that an ack will be allowed
        var res = await duplexPipe.Application.Input.ReadAsync();
        duplexPipe.Application.Input.AdvanceTo(res.Buffer.Start, res.Buffer.Start);

        var buffer = new byte[FrameSize];
        // Only ack 26 of 31 bytes
        WriteFrame(buffer, 0, FrameSize + 2);
        await duplexPipe.Application.Output.WriteAsync(buffer);

        // Updates ack from App.Output in App.Input
        res = await duplexPipe.Transport.Input.ReadAsync();
        duplexPipe.Transport.Input.AdvanceTo(res.Buffer.End);

        var reader = (AckPipeReader)duplexPipe.Application.Input;
        reader.Resend();

        res = await duplexPipe.Application.Input.ReadAsync();

        Assert.Equal(5, res.Buffer.Length);
        Assert.True(res.IsCanceled);
        Assert.False(res.IsCompleted);
        Assert.Equal(new byte[] { 3, 4, 5, 6, 7 }, res.Buffer.ToArray());

        duplexPipe.Application.Input.AdvanceTo(res.Buffer.End);

        // smoke testing that we can still receive
        await duplexPipe.Transport.Output.WriteAsync(new byte[2] { 9, 7 });

        res = await duplexPipe.Application.Input.ReadAsync();

        Assert.Equal(FrameSize + 2, res.Buffer.Length);
        var (len, ackId) = ReadFrame(res.Buffer.ToArray());
        Assert.Equal(2, len);
        Assert.Equal(0, ackId);
        Assert.Equal(new byte[] { 9, 7 }, res.Buffer.ToArray().AsSpan(FrameSize, 2).ToArray());
        Assert.False(res.IsCanceled);
        Assert.False(res.IsCompleted);
    }

    [Fact]
    public async Task BackpressureIsAppliedInBothDirections()
    {
        var duplexPipe = CreateClient(inputOptions: new PipeOptions(pauseWriterThreshold: 10, resumeWriterThreshold: 5, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline),
            outputOptions: new PipeOptions(pauseWriterThreshold: 10, resumeWriterThreshold: 5, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline));

        var buffer = new byte[FrameSize + 1];
        WriteFrame(buffer, 1, 0);
        var writeTask = duplexPipe.Application.Output.WriteAsync(buffer);
        // Shouldn't complete until the reader reads due to pauseWriterThreshold being 10 and we wrote 25
        Assert.False(writeTask.IsCompleted);

        var res = await duplexPipe.Transport.Input.ReadAsync();
        Assert.Equal(1, res.Buffer.Length);
        duplexPipe.Transport.Input.AdvanceTo(res.Buffer.Start, res.Buffer.End);
        await writeTask.DefaultTimeout();

        writeTask = duplexPipe.Transport.Output.WriteAsync(new byte[2] { 4, 5 });
        // Shouldn't complete until the reader reads due to pauseWriterThreshold being 10 and we wrote 26
        Assert.False(writeTask.IsCompleted);

        res = await duplexPipe.Application.Input.ReadAsync();
        Assert.Equal(26, res.Buffer.Length);
        duplexPipe.Application.Input.AdvanceTo(res.Buffer.End);
        await writeTask.DefaultTimeout();
    }

    internal static DuplexPipePair CreateClient(PipeOptions inputOptions = default, PipeOptions outputOptions = default)
    {
        var input = new Pipe(inputOptions ?? new());
        var output = new Pipe(outputOptions ?? new());

        // Use for one side only, this is client side
        var ackWriter = new AckPipeWriter(output);
        var ackReader = new AckPipeReader(output);
        var transportReader = new ParseAckPipeReader(input.Reader, ackWriter, ackReader);
        var transportToApplication = new DuplexPipe(ackReader, input.Writer);
        var applicationToTransport = new DuplexPipe(transportReader, ackWriter);

        // Transport.Output.Write goes to Application.Input, which is read in the transport code
        // Application.Output.Write goes to Transport.Input, which is read in the application code
        
        return new DuplexPipePair(applicationToTransport, transportToApplication);
    }

    internal static DuplexPipePair CreateServer(PipeOptions inputOptions = default, PipeOptions outputOptions = default)
    {
        var input = new Pipe(inputOptions ?? new());
        var output = new Pipe(outputOptions ?? new());

        // Use for one side only, this is server side
        var ackWriter = new AckPipeWriter(output);
        var ackReader = new AckPipeReader(output);
        var transportReader = new ParseAckPipeReader(input.Reader, ackWriter, ackReader);
        var transportToApplication = new DuplexPipe(ackReader, input.Writer);
        var applicationToTransport = new DuplexPipe(transportReader, ackWriter);

        return new DuplexPipePair(transportToApplication, applicationToTransport);
    }

    internal static void WriteFrame(byte[] header, long payloadLength, long ackId = 0)
    {
        Assert.True(header.Length >= FrameSize);

        Assert.True(BitConverter.TryWriteBytes(header, payloadLength));
        Assert.True(BitConverter.TryWriteBytes(header.AsSpan(8), ackId));
        var res = BitConverter.TryWriteBytes(header.AsSpan(), payloadLength);
        Debug.Assert(res);
        var status = Base64.EncodeToUtf8InPlace(header.AsSpan(), 8, out var written);
        Debug.Assert(status == OperationStatus.Done);
        Debug.Assert(written == 12);
        res = BitConverter.TryWriteBytes(header.AsSpan(12), ackId);
        Debug.Assert(res);
        status = Base64.EncodeToUtf8InPlace(header.AsSpan(12), 8, out written);
        Debug.Assert(status == OperationStatus.Done);
        Debug.Assert(written == 12);
    }

    internal static (long Length, long AckId) ReadFrame(byte[] frameBytes)
    {
        var frame = frameBytes.AsSpan(0, FrameSize);
        Span<byte> buffer = stackalloc byte[FrameSize];
        frame.CopyTo(buffer);
        var status = Base64.DecodeFromUtf8InPlace(buffer.Slice(0, 12), out var written);
        Assert.Equal(OperationStatus.Done, status);
        Assert.Equal(8, written);
        var len = BitConverter.ToInt64(buffer);
        status = Base64.DecodeFromUtf8InPlace(buffer.Slice(12, 12), out written);
        Assert.Equal(OperationStatus.Done, status);
        Assert.Equal(8, written);
        var ackId = BitConverter.ToInt64(buffer.Slice(12));

        return (len, ackId);
    }

    internal static (long PayloadLength, long AckId) ReadFrame(ref Span<byte> header)
    {
        Assert.True(header.Length >= FrameSize);

        var len = BitConverter.ToInt64(header);
        var ackId = BitConverter.ToInt64(header.Slice(FrameSize / 2));

        return (len, ackId);
    }

    internal static DuplexPipePair CreateConnectionPair(PipeOptions inputOptions, PipeOptions outputOptions)
    {
        var input = new Pipe(inputOptions);
        var output = new Pipe(outputOptions);

        // wire up both sides for testing
        var ackWriterApp = new AckPipeWriter(output);
        var ackReaderApp = new AckPipeReader(output);
        var ackWriterClient = new AckPipeWriter(input);
        var ackReaderClient = new AckPipeReader(input);
        var transportReader = new ParseAckPipeReader(input.Reader, ackWriterApp, ackReaderApp);
        var applicationReader = new ParseAckPipeReader(ackReaderApp, ackWriterClient, ackReaderClient);
        var transportToApplication = new DuplexPipe(applicationReader, ackWriterClient);
        var applicationToTransport = new DuplexPipe(transportReader, ackWriterApp);

        return new DuplexPipePair(applicationToTransport, transportToApplication);
    }

    internal sealed class DuplexPipe : IDuplexPipe
    {
        public DuplexPipe(PipeReader reader, PipeWriter writer)
        {
            Input = reader;
            Output = writer;
        }

        public PipeReader Input { get; }

        public PipeWriter Output { get; }
    }

    public readonly struct DuplexPipePair
    {
        public IDuplexPipe Transport { get; }
        public IDuplexPipe Application { get; }

        public DuplexPipePair(IDuplexPipe transport, IDuplexPipe application)
        {
            Transport = transport;
            Application = application;
        }
    }
}

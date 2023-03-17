// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PipelinesOverNetwork;
using static PipelinesOverNetwork.AckDuplexPipe;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol;

public class AckPipeTests
{
    [Fact]
    public async Task CanSendAndReceiveTransport()
    {
        var duplexPipe = AckDuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions());

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
        var duplexPipe = AckDuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions());

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
        var duplexPipe = AckDuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions());

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

        var buffer = new byte[20];
        WriteFrame(buffer, buffer.Length - 16, 0);
        buffer[16] = 9;
        buffer[17] = 9;
        buffer[18] = 9;
        buffer[19] = 9;

        // "write" from server
        await duplexPipe.Application.Output.WriteAsync(buffer);

        // read in client application layer
        var res = await duplexPipe.Transport.Input.ReadAsync();
        Assert.Equal(4, res.Buffer.Length);
        Assert.Equal(buffer.AsSpan(16).ToArray(), res.Buffer.ToArray());
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
        Assert.Equal(buffer.Length + 16, res.Buffer.Length);
        Assert.Equal(buffer, res.Buffer.Slice(16).ToArray());
    }

    internal static DuplexPipePair CreateClient(PipeOptions inputOptions = default, PipeOptions outputOptions = default)
    {
        var input = new Pipe(inputOptions ?? new());
        var output = new Pipe(outputOptions ?? new());

        // Use for one side only, this is client side
        var ackWriter = new AckPipeWriter(output.Writer);
        var ackReader = new AckPipeReader(output.Reader);
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
        var ackWriter = new AckPipeWriter(output.Writer);
        var ackReader = new AckPipeReader(output.Reader);
        var transportReader = new ParseAckPipeReader(input.Reader, ackWriter, ackReader);
        var transportToApplication = new DuplexPipe(ackReader, input.Writer);
        var applicationToTransport = new DuplexPipe(transportReader, ackWriter);

        return new DuplexPipePair(transportToApplication, applicationToTransport);
    }

    internal static void WriteFrame(byte[] header, long payloadLength, long ackId = 0)
    {
        Assert.True(header.Length >= 16);

        Assert.True(BitConverter.TryWriteBytes(header, payloadLength));
        Assert.True(BitConverter.TryWriteBytes(header.AsSpan(8), ackId));
    }

    internal static (long Length, long AckId) ReadFrame(byte[] buffer)
    {
        var len = BitConverter.ToInt64(buffer);
        var ackId = BitConverter.ToInt64(buffer.AsSpan(8));

        return (len, ackId);
    }

    internal static (long PayloadLength, long AckId) ReadFrame(ref Span<byte> header)
    {
        Assert.True(header.Length >= 16);

        var len = BitConverter.ToInt64(header);
        var ackId = BitConverter.ToInt64(header.Slice(8));

        return (len, ackId);
    }
}

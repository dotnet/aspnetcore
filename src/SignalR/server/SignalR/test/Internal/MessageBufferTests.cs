// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.SignalR.Tests.Internal;

public class MessageBufferTests
{
    [Fact]
    public async Task CanWriteNonBufferedMessagesWithoutBlocking()
    {
        var protocol = new JsonHubProtocol();
        var connection = new TestConnectionContext();
        var pipes = DuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions());
        connection.Transport = pipes.Transport;
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 1);

        for (var i = 0; i < 100; i++)
        {
            await messageBuffer.WriteAsync(new SerializedHubMessage(PingMessage.Instance), default).DefaultTimeout();
        }

        var count = 0;
        while (count < 100)
        { 
            var res = await pipes.Application.Input.ReadAsync().DefaultTimeout();

            var buffer = res.Buffer;
            Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out var message));
            Assert.IsType<PingMessage>(message);

            pipes.Application.Input.AdvanceTo(buffer.Start);
            count++;
        }
    }

    [Fact]
    public async Task WriteBlocksOnAckWhenBufferFull()
    {
        var protocol = new JsonHubProtocol();
        var connection = new TestConnectionContext();
        var pipes = DuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions(pauseWriterThreshold: 200000, resumeWriterThreshold: 100000));
        connection.Transport = pipes.Transport;
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 100_000);

        await messageBuffer.WriteAsync(new SerializedHubMessage(new InvocationMessage("t", new object[] { new byte[100000] })), default);

        var writeTask = messageBuffer.WriteAsync(new SerializedHubMessage(new StreamItemMessage("id", null)), default);
        Assert.False(writeTask.IsCompleted);

        var res = await pipes.Application.Input.ReadAsync();

        var buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out var message));
        Assert.IsType<InvocationMessage>(message);

        pipes.Application.Input.AdvanceTo(buffer.Start);

        // Write not unblocked by read, only unblocked after ack received
        Assert.False(writeTask.IsCompleted);

        messageBuffer.Ack(new AckMessage(1));
        await writeTask.DefaultTimeout();

        res = await pipes.Application.Input.ReadAsync().DefaultTimeout();

        buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out message));
        Assert.IsType<StreamItemMessage>(message);

        pipes.Application.Input.AdvanceTo(buffer.Start);
    }

    [Fact]
    public async Task UnAckedMessageResentOnReconnect()
    {
        var protocol = new JsonHubProtocol();
        var connection = new TestConnectionContext();
        var pipes = DuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions());
        connection.Transport = pipes.Transport;
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 1000);

        await messageBuffer.WriteAsync(new SerializedHubMessage(new StreamItemMessage("id", null)), default);

        var res = await pipes.Application.Input.ReadAsync();

        var buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out var message));
        Assert.IsType<StreamItemMessage>(message);

        pipes.Application.Input.AdvanceTo(buffer.Start);

        DuplexPipe.UpdateConnectionPair(ref pipes, connection);
        messageBuffer.Resend();

        // Any message except SequenceMessage will be ignored until a SequenceMessage is received
        Assert.False(messageBuffer.ShouldProcessMessage(PingMessage.Instance));
        Assert.False(messageBuffer.ShouldProcessMessage(CompletionMessage.WithResult("1", null)));
        Assert.True(messageBuffer.ShouldProcessMessage(new SequenceMessage(1)));

        res = await pipes.Application.Input.ReadAsync();

        buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out message));
        var seqMessage = Assert.IsType<SequenceMessage>(message);
        Assert.Equal(1, seqMessage.SequenceId);

        pipes.Application.Input.AdvanceTo(buffer.Start);

        res = await pipes.Application.Input.ReadAsync();

        buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out message));
        Assert.IsType<StreamItemMessage>(message);

        pipes.Application.Input.AdvanceTo(buffer.Start);

        messageBuffer.ResetSequence(new SequenceMessage(1));

        Assert.True(messageBuffer.ShouldProcessMessage(PingMessage.Instance));
        Assert.True(messageBuffer.ShouldProcessMessage(CompletionMessage.WithResult("1", null)));
    }

    [Fact]
    public async Task AckedMessageNotResentOnReconnect()
    {
        var protocol = new JsonHubProtocol();
        var connection = new TestConnectionContext();
        var pipes = DuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions());
        connection.Transport = pipes.Transport;
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 1000);

        await messageBuffer.WriteAsync(new SerializedHubMessage(new StreamItemMessage("id", null)), default);

        var res = await pipes.Application.Input.ReadAsync();

        var buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out var message));
        Assert.IsType<StreamItemMessage>(message);

        pipes.Application.Input.AdvanceTo(buffer.Start);

        messageBuffer.Ack(new AckMessage(1));

        DuplexPipe.UpdateConnectionPair(ref pipes, connection);
        messageBuffer.Resend();

        res = await pipes.Application.Input.ReadAsync();

        buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out message));
        var seqMessage = Assert.IsType<SequenceMessage>(message);
        Assert.Equal(2, seqMessage.SequenceId);

        pipes.Application.Input.AdvanceTo(buffer.Start);

        await messageBuffer.WriteAsync(new SerializedHubMessage(CompletionMessage.WithResult("1", null)), default);

        res = await pipes.Application.Input.ReadAsync();

        buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out message));
        Assert.IsType<CompletionMessage>(message);

        pipes.Application.Input.AdvanceTo(buffer.Start);
    }

    [Fact]
    public async Task ReceiveSequenceMessageWithLargerIDThanMessagesReceived()
    {
        var protocol = new JsonHubProtocol();
        var connection = new TestConnectionContext();
        var pipes = DuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions());
        connection.Transport = pipes.Transport;
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 1000);

        DuplexPipe.UpdateConnectionPair(ref pipes, connection);
        messageBuffer.Resend();

        var res = await pipes.Application.Input.ReadAsync();

        var buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out var message));
        var seqMessage = Assert.IsType<SequenceMessage>(message);
        Assert.Equal(1, seqMessage.SequenceId);

        pipes.Application.Input.AdvanceTo(buffer.Start);

        Assert.Throws<InvalidOperationException>(() => messageBuffer.ResetSequence(new SequenceMessage(2)));
    }

    [Fact]
    public async Task WriteManyMessagesAckSomeProperlyBuffers()
    {
        var protocol = new JsonHubProtocol();
        var connection = new TestConnectionContext();
        var pipes = DuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions());
        connection.Transport = pipes.Transport;
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 100_000);

        for (var i = 0; i < 1000; i++)
        {
            await messageBuffer.WriteAsync(new SerializedHubMessage(new StreamItemMessage("1", null)), default).DefaultTimeout();
        }

        var ackNum = Random.Shared.Next(0, 1000);
        messageBuffer.Ack(new AckMessage(ackNum));

        DuplexPipe.UpdateConnectionPair(ref pipes, connection);
        messageBuffer.Resend();

        var res = await pipes.Application.Input.ReadAsync();

        var buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out var message));
        var seqMessage = Assert.IsType<SequenceMessage>(message);
        Assert.Equal(ackNum + 1, seqMessage.SequenceId);

        pipes.Application.Input.AdvanceTo(buffer.Start);

        for (var i = 0; i < 1000 - ackNum; i++)
        {
            res = await pipes.Application.Input.ReadAsync();

            buffer = res.Buffer;
            Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out message));
            Assert.IsType<StreamItemMessage>(message);

            pipes.Application.Input.AdvanceTo(buffer.Start);
        }
    }

    [Fact]
    public async Task MessageBufferLimitCanBeModified()
    {
        var protocol = new JsonHubProtocol();
        var connection = new TestConnectionContext();
        var pipes = DuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions());
        connection.Transport = pipes.Transport;
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 1);

        await messageBuffer.WriteAsync(new SerializedHubMessage(new InvocationMessage("t", new object[] { 1 })), default);

        var writeTask = messageBuffer.WriteAsync(new SerializedHubMessage(new StreamItemMessage("id", null)), default);
        Assert.False(writeTask.IsCompleted);

        var res = await pipes.Application.Input.ReadAsync();

        var buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out var message));
        Assert.IsType<InvocationMessage>(message);

        pipes.Application.Input.AdvanceTo(buffer.Start);

        // Write not unblocked by read, only unblocked after ack received
        Assert.False(writeTask.IsCompleted);

        messageBuffer.Ack(new AckMessage(1));
        await writeTask.DefaultTimeout();

        res = await pipes.Application.Input.ReadAsync().DefaultTimeout();

        buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out message));
        Assert.IsType<StreamItemMessage>(message);

        pipes.Application.Input.AdvanceTo(buffer.Start);
    }
}

internal sealed class TestConnectionContext : ConnectionContext
{
    public override string ConnectionId { get; set; }
    public override IFeatureCollection Features { get; } = new FeatureCollection();
    public override IDictionary<object, object> Items { get; set; }
    public override IDuplexPipe Transport { get; set; }
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

    public static DuplexPipePair CreateConnectionPair(PipeOptions inputOptions, PipeOptions outputOptions)
    {
        var input = new Pipe(inputOptions);
        var output = new Pipe(outputOptions);

        var transportToApplication = new DuplexPipe(output.Reader, input.Writer);
        var applicationToTransport = new DuplexPipe(input.Reader, output.Writer);

        return new DuplexPipePair(applicationToTransport, transportToApplication);
    }

    // This class exists to work around issues with value tuple on .NET Framework
    public struct DuplexPipePair
    {
        public IDuplexPipe Transport { get; set; }
        public IDuplexPipe Application { get; set; }

        public DuplexPipePair(IDuplexPipe transport, IDuplexPipe application)
        {
            Transport = transport;
            Application = application;
        }
    }

    public static void UpdateConnectionPair(ref DuplexPipePair duplexPipePair, ConnectionContext connection)
    {
        var prevPipe = duplexPipePair.Application.Input;
        var input = new Pipe();

        // Add new pipe for reading from and writing to transport from app code
        var transportToApplication = new DuplexPipe(duplexPipePair.Transport.Input, input.Writer);
        var applicationToTransport = new DuplexPipe(input.Reader, duplexPipePair.Application.Output);

        duplexPipePair.Application = applicationToTransport;
        duplexPipePair.Transport = transportToApplication;

        connection.Transport = duplexPipePair.Transport;

        // Close previous pipe with specific error that application code can catch to know a restart is occurring
        prevPipe.Complete(new ConnectionResetException(""));
    }
}

internal sealed class TestBinder : IInvocationBinder
{
    public IReadOnlyList<Type> GetParameterTypes(string methodName)
    {
        var list = new List<Type>
        {
            typeof(object)
        };
        return list;
    }

    public Type GetReturnType(string invocationId)
    {
        return typeof(object);
    }

    public Type GetStreamItemType(string streamId)
    {
        return typeof(object);
    }
}

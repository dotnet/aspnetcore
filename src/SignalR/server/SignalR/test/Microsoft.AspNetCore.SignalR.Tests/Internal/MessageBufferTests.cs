// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

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
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 1, NullLogger.Instance);

        for (var i = 0; i < 100; i++)
        {
            await messageBuffer.WriteAsync(PingMessage.Instance, default).DefaultTimeout();
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
    public async Task WriteBlocksOnAckWhenMessageBufferFull()
    {
        var protocol = new JsonHubProtocol();
        var connection = new TestConnectionContext();
        var pipes = DuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions(pauseWriterThreshold: 200000, resumeWriterThreshold: 100000));
        connection.Transport = pipes.Transport;
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 100_000, NullLogger.Instance);

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

        await messageBuffer.AckAsync(new AckMessage(1));
        await writeTask.DefaultTimeout();

        res = await pipes.Application.Input.ReadAsync().DefaultTimeout();

        buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out message));
        Assert.IsType<StreamItemMessage>(message);

        pipes.Application.Input.AdvanceTo(buffer.Start);
    }

    [Fact]
    public async Task BackpressureWriteMessageSurvivesReconnect()
    {
        var protocol = new JsonHubProtocol();
        var connection = new TestConnectionContext();
        var pipeOptions = new PipeOptions(pauseWriterThreshold: 100, resumeWriterThreshold: 50);
        var pipes = DuplexPipe.CreateConnectionPair(new PipeOptions(), pipeOptions);
        connection.Transport = pipes.Transport;
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 100_000, NullLogger.Instance);

        await messageBuffer.WriteAsync(new SerializedHubMessage(new InvocationMessage("t", new object[] { new byte[40] })), default);

        // Write will hit pipe backpressure
        var writeTask = messageBuffer.WriteAsync(new SerializedHubMessage(new InvocationMessage("t", new object[] { new byte[40] })), default);
        Assert.False(writeTask.IsCompleted);

        DuplexPipe.UpdateConnectionPair(ref pipes, connection, pipeOptions);
        var resendTask = messageBuffer.ResendAsync(pipes.Transport.Output);

        var res = await pipes.Application.Input.ReadAsync();
        var buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out var message));
        Assert.IsType<SequenceMessage>(message);

        pipes.Application.Input.AdvanceTo(buffer.Start);

        for (var i = 0; i < 2; i++)
        {
            res = await pipes.Application.Input.ReadAsync();
            buffer = res.Buffer;
            Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out message));
            Assert.IsType<InvocationMessage>(message);

            pipes.Application.Input.AdvanceTo(buffer.Start);
        }

        Assert.False(pipes.Application.Input.TryRead(out res));

        await resendTask;
    }

    [Fact]
    public async Task UnAckedMessageResentOnReconnect()
    {
        var protocol = new JsonHubProtocol();
        var connection = new TestConnectionContext();
        var pipes = DuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions());
        connection.Transport = pipes.Transport;
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 1000, NullLogger.Instance);

        await messageBuffer.WriteAsync(new StreamItemMessage("id", null), default);

        var res = await pipes.Application.Input.ReadAsync();

        var buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out var message));
        Assert.IsType<StreamItemMessage>(message);

        pipes.Application.Input.AdvanceTo(buffer.Start);

        DuplexPipe.UpdateConnectionPair(ref pipes, connection);
        await messageBuffer.ResendAsync(pipes.Transport.Output);

        Assert.True(messageBuffer.ShouldProcessMessage(PingMessage.Instance));
        Assert.True(messageBuffer.ShouldProcessMessage(CompletionMessage.WithResult("1", null)));
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

        messageBuffer.ShouldProcessMessage(new SequenceMessage(1));

        Assert.True(messageBuffer.ShouldProcessMessage(PingMessage.Instance));
        Assert.False(messageBuffer.ShouldProcessMessage(CompletionMessage.WithResult("1", null)));
    }

    [Fact]
    public async Task AckedMessageNotResentOnReconnect()
    {
        var protocol = new JsonHubProtocol();
        var connection = new TestConnectionContext();
        var pipes = DuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions());
        connection.Transport = pipes.Transport;
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 1000, NullLogger.Instance);

        await messageBuffer.WriteAsync(new StreamItemMessage("id", null), default);

        var res = await pipes.Application.Input.ReadAsync();

        var buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out var message));
        Assert.IsType<StreamItemMessage>(message);

        pipes.Application.Input.AdvanceTo(buffer.Start);

        await messageBuffer.AckAsync(new AckMessage(1));

        DuplexPipe.UpdateConnectionPair(ref pipes, connection);
        await messageBuffer.ResendAsync(pipes.Transport.Output);

        res = await pipes.Application.Input.ReadAsync();

        buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out message));
        var seqMessage = Assert.IsType<SequenceMessage>(message);
        Assert.Equal(2, seqMessage.SequenceId);

        pipes.Application.Input.AdvanceTo(buffer.Start);

        await messageBuffer.WriteAsync(CompletionMessage.WithResult("1", null), default);

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
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 1000, NullLogger.Instance);

        DuplexPipe.UpdateConnectionPair(ref pipes, connection);
        await messageBuffer.ResendAsync(pipes.Transport.Output);

        var res = await pipes.Application.Input.ReadAsync();

        var buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out var message));
        var seqMessage = Assert.IsType<SequenceMessage>(message);
        Assert.Equal(1, seqMessage.SequenceId);

        pipes.Application.Input.AdvanceTo(buffer.Start);

        Assert.Throws<InvalidOperationException>(() => messageBuffer.ShouldProcessMessage(new SequenceMessage(2)));
    }

    [Fact]
    public async Task WriteManyMessagesAckSomeProperlyBuffers()
    {
        var protocol = new JsonHubProtocol();
        var connection = new TestConnectionContext();
        var pipes = DuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions());
        connection.Transport = pipes.Transport;
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 100_000, NullLogger.Instance);

        for (var i = 0; i < 1000; i++)
        {
            await messageBuffer.WriteAsync(new StreamItemMessage("1", null), default).DefaultTimeout();
        }

        var ackNum = Random.Shared.Next(0, 1000);
        await messageBuffer.AckAsync(new AckMessage(ackNum));

        DuplexPipe.UpdateConnectionPair(ref pipes, connection);
        await messageBuffer.ResendAsync(pipes.Transport.Output);

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

        Assert.False(pipes.Application.Input.TryRead(out res));
    }

    [Fact]
    public async Task MessageBufferLimitCanBeModified()
    {
        var protocol = new JsonHubProtocol();
        var connection = new TestConnectionContext();
        var pipes = DuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions());
        connection.Transport = pipes.Transport;
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 1, NullLogger.Instance);

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

        await messageBuffer.AckAsync(new AckMessage(1));
        await writeTask.DefaultTimeout();

        res = await pipes.Application.Input.ReadAsync().DefaultTimeout();

        buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out message));
        Assert.IsType<StreamItemMessage>(message);

        pipes.Application.Input.AdvanceTo(buffer.Start);
    }

    [Fact]
    public async Task CanSendMessagesWhilePipeClosed()
    {
        var protocol = new JsonHubProtocol();
        var connection = new TestConnectionContext();
        var pipes = DuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions());
        connection.Transport = pipes.Transport;
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 100_000, NullLogger.Instance);

        await messageBuffer.WriteAsync(new StreamItemMessage("1", null), default);

        // simulate connection closing
        pipes.Application.Input.Complete();

        // send while connection down
        await messageBuffer.WriteAsync(new StreamItemMessage("1", null), default);
        await messageBuffer.WriteAsync(new StreamItemMessage("1", null), default);

        // simulate reconnect
        DuplexPipe.UpdateConnectionPair(ref pipes, connection);
        await messageBuffer.ResendAsync(pipes.Transport.Output);

        var res = await pipes.Application.Input.ReadAsync();
        var buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out var message));
        Assert.IsType<SequenceMessage>(message);

        pipes.Application.Input.AdvanceTo(buffer.Start);

        for (var i = 0; i < 3; i++)
        {
            res = await pipes.Application.Input.ReadAsync();
            buffer = res.Buffer;
            Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out message));
            Assert.IsType<StreamItemMessage>(message);

            pipes.Application.Input.AdvanceTo(buffer.Start);
        }

        Assert.False(pipes.Application.Input.TryRead(out res));
    }

    [Fact]
    public async Task AckMessagesSentAutomatically()
    {
        var protocol = new JsonHubProtocol();
        var connection = new TestConnectionContext();
        var pipes = DuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions());
        connection.Transport = pipes.Transport;
        var timeProvider = new FakeTimeProvider();
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 100_000, NullLogger.Instance, timeProvider);

        // Simulate receiving messages
        Assert.True(messageBuffer.ShouldProcessMessage(new StreamItemMessage("1", null)));
        Assert.True(messageBuffer.ShouldProcessMessage(new StreamItemMessage("1", null)));

        timeProvider.Advance(MessageBuffer.AckRate);

        var res = await pipes.Application.Input.ReadAsync();
        var buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out var message));
        var ackMessage = Assert.IsType<AckMessage>(message);
        pipes.Application.Input.AdvanceTo(buffer.Start);

        Assert.Equal(2, ackMessage.SequenceId);

        Assert.True(messageBuffer.ShouldProcessMessage(new StreamItemMessage("1", null)));

        timeProvider.Advance(MessageBuffer.AckRate);

        res = await pipes.Application.Input.ReadAsync();
        buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out message));
        ackMessage = Assert.IsType<AckMessage>(message);
        pipes.Application.Input.AdvanceTo(buffer.Start);

        Assert.Equal(3, ackMessage.SequenceId);
    }

    [Fact]
    public async Task ReceiveAckDuringResendStillSendsAllMessages()
    {
        var protocol = new JsonHubProtocol();
        var connection = new TestConnectionContext();
        var pipeOptions = new PipeOptions(pauseWriterThreshold: 250, resumeWriterThreshold: 120);
        var pipes = DuplexPipe.CreateConnectionPair(new PipeOptions(), pipeOptions);
        connection.Transport = pipes.Transport;
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 100_000, NullLogger.Instance);

        await messageBuffer.WriteAsync(new SerializedHubMessage(new InvocationMessage("t", new object[] { new byte[10] })), default);
        await messageBuffer.WriteAsync(new SerializedHubMessage(new InvocationMessage("t", new object[] { new byte[100] })), default).DefaultTimeout();
        var writeTask = messageBuffer.WriteAsync(new SerializedHubMessage(new InvocationMessage("t", new object[] { new byte[100] })), default);

        // simulate reconnect
        // Smaller PipeOptions on reconnect to force the Resend loop to pause on sending the 2nd message
        DuplexPipe.UpdateConnectionPair(ref pipes, connection, new PipeOptions(pauseWriterThreshold: 100, resumeWriterThreshold: 50));
        var resendTask = messageBuffer.ResendAsync(pipes.Transport.Output);

        Assert.True(messageBuffer.ShouldProcessMessage(new SequenceMessage(1)));
        // Ack all 3 messages while the Resend loop is running, Resend should continue sending all messages
        var ackTask = messageBuffer.AckAsync(new AckMessage(3));

        var res = await pipes.Application.Input.ReadAsync();
        var buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out var message));
        Assert.IsType<SequenceMessage>(message);
        pipes.Application.Input.AdvanceTo(buffer.Start);

        for (var i = 0; i < 3; i++)
        {
            res = await pipes.Application.Input.ReadAsync();
            buffer = res.Buffer;
            Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out message));
            Assert.IsType<InvocationMessage>(message);
            pipes.Application.Input.AdvanceTo(buffer.Start);
        }

        await resendTask;
        await ackTask;
    }

    [Fact]
    public async Task SendingAckMessageDelayedDuringResend()
    {
        var protocol = new JsonHubProtocol();
        var connection = new TestConnectionContext();
        var pipeOptions = new PipeOptions(pauseWriterThreshold: 100, resumeWriterThreshold: 50);
        var pipes = DuplexPipe.CreateConnectionPair(new PipeOptions(), pipeOptions);
        connection.Transport = pipes.Transport;
        var timeProvider = new FakeTimeProvider();
        using var messageBuffer = new MessageBuffer(connection, protocol, bufferLimit: 100_000, NullLogger.Instance, timeProvider);

        await messageBuffer.WriteAsync(new SerializedHubMessage(new InvocationMessage("t", new object[] { new byte[10] })), default);
        var writeTask = messageBuffer.WriteAsync(new SerializedHubMessage(new InvocationMessage("t", new object[] { new byte[100] })), default).DefaultTimeout();

        // simulate reconnect
        DuplexPipe.UpdateConnectionPair(ref pipes, connection, pipeOptions);
        var resendTask = messageBuffer.ResendAsync(pipes.Transport.Output);

        // Simulate receiving messages
        Assert.True(messageBuffer.ShouldProcessMessage(new SequenceMessage(1)));
        Assert.True(messageBuffer.ShouldProcessMessage(new StreamItemMessage("1", null)));
        Assert.True(messageBuffer.ShouldProcessMessage(new StreamItemMessage("1", null)));

        Assert.False(resendTask.IsCompleted);

        // Trigger sending an AckMessage while Resend is running
        timeProvider.Advance(MessageBuffer.AckRate);

        var res = await pipes.Application.Input.ReadAsync();
        var buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out var message));
        Assert.IsType<SequenceMessage>(message);
        pipes.Application.Input.AdvanceTo(buffer.Start);

        timeProvider.Advance(MessageBuffer.AckRate);

        for (var i = 0; i < 2; i++)
        {
            res = await pipes.Application.Input.ReadAsync();
            buffer = res.Buffer;
            Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out message));
            Assert.IsType<InvocationMessage>(message);
            pipes.Application.Input.AdvanceTo(buffer.Start);
        }

        await resendTask;

        res = await pipes.Application.Input.ReadAsync();
        buffer = res.Buffer;
        Assert.True(protocol.TryParseMessage(ref buffer, new TestBinder(), out message));
        Assert.IsType<AckMessage>(message);
        pipes.Application.Input.AdvanceTo(buffer.Start);

        Assert.False(pipes.Application.Input.TryRead(out res));
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

    public static void UpdateConnectionPair(ref DuplexPipePair duplexPipePair, ConnectionContext connection, PipeOptions pipeOptions = null)
    {
        var input = new Pipe(pipeOptions ?? new PipeOptions());

        // Add new pipe for reading from and writing to transport from app code
        var transportToApplication = new DuplexPipe(duplexPipePair.Transport.Input, input.Writer);
        var applicationToTransport = new DuplexPipe(input.Reader, duplexPipePair.Application.Output);

        duplexPipePair.Application = applicationToTransport;
        duplexPipePair.Transport = transportToApplication;

        connection.Transport = duplexPipePair.Transport;
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

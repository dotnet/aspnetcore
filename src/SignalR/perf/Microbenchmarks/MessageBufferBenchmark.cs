// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Microbenchmarks.Shared;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class MessageBufferBenchmark
{
    private const int BurstSize = 32;

    private MessageBuffer _buffer;
    private SerializedHubMessage _message;
    private JsonHubProtocol _protocol;
    private AckMessage _ack;
    private PipeReader _input;

    [Params(0, 10, 100, 1000)]
    public int UnacknowledgedMessages;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _protocol = new JsonHubProtocol();
        _message = new SerializedHubMessage(new InvocationMessage("Target", []));
        _message.GetSerializedMessage(_protocol);
        _ack = new AckMessage(0);
        _input = PipeReader.Create(Stream.Null);
        var connection = new TestConnectionContext
        {
            ConnectionId = "benchmark",
            Transport = new DuplexPipe(_input, PipeWriter.Create(Stream.Null)),
        };

        // Isolate buffer bookkeeping from transport backpressure and byte-limit waits.
        _buffer = new MessageBuffer(connection, _protocol, long.MaxValue, NullLogger.Instance);
        for (var i = 0; i < UnacknowledgedMessages; i++)
        {
            _buffer.WriteAsync(_message, default).GetAwaiter().GetResult();
        }
    }

    [Benchmark]
    public void AppendAndAcknowledge()
    {
        _buffer.WriteAsync(_message, default).GetAwaiter().GetResult();

        // Acknowledge the oldest message, keeping the backlog constant across invocations.
        _ack.SequenceId++;
        _buffer.AckAsync(_ack).GetAwaiter().GetResult();
    }

    [Benchmark(OperationsPerInvoke = BurstSize)]
    public void BurstAndAcknowledge()
    {
        for (var i = 0; i < BurstSize; i++)
        {
            _buffer.WriteAsync(_message, default).GetAwaiter().GetResult();
        }

        // Acknowledge once per burst; results are normalized per appended message.
        _ack.SequenceId += BurstSize;
        _buffer.AckAsync(_ack).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        using var output = new MemoryStream();
        var writer = PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true));
        try
        {
            _buffer.ResendAsync(writer).GetAwaiter().GetResult();
            var data = new ReadOnlySequence<byte>(output.GetBuffer().AsMemory(0, (int)output.Length));
            var binder = new TestBinder(Array.Empty<Type>());
            if (!_protocol.TryParseMessage(ref data, binder, out var firstMessage)
                || firstMessage is not SequenceMessage sequence
                || sequence.SequenceId != _ack.SequenceId + 1)
            {
                throw new InvalidOperationException("The replay sequence does not match the acknowledgement position.");
            }

            var count = 0;
            while (_protocol.TryParseMessage(ref data, binder, out var message))
            {
                if (message is not InvocationMessage)
                {
                    throw new InvalidOperationException("An unexpected message was buffered.");
                }

                count++;
            }

            if (!data.IsEmpty || count != UnacknowledgedMessages)
            {
                throw new InvalidOperationException("The unacknowledged backlog changed during the benchmark.");
            }
        }
        finally
        {
            writer.Complete();
            _buffer.Dispose();
            _input.Complete();
        }
    }
}

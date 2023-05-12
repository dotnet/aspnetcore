// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal sealed class MessageBuffer
{
    private readonly (SerializedHubMessage? Message, long? SequenceId)[] _buffer;
    private int _index;

    // TODO: pass in limits
    public MessageBuffer()
    {
        _buffer = new (SerializedHubMessage? Message, long? SequenceId)[10];
    }

    public async ValueTask<FlushResult> WriteAsync(PipeWriter pipeWriter, SerializedHubMessage hubMessage, IHubProtocol protocol,
        CancellationToken cancellationToken)
    {
        // No lock because this is always called in a single async loop?
        // And other methods don't affect the checks here?

        // TODO: Backpressure

        if (_buffer[_index].Message is not null)
        {
            // ...
        }

        long? sequenceId;
        if (hubMessage.Message is HubInvocationMessage invocationMessage)
        {
            sequenceId = invocationMessage.SequenceId;
        }
        else
        {
            // Non-ackable message, don't add to buffer
            return await pipeWriter.WriteAsync(hubMessage.GetSerializedMessage(protocol), cancellationToken);
        }

        _buffer[_index] = (hubMessage, sequenceId);
        _index = (_index + 1) % _buffer.Length;
        return await pipeWriter.WriteAsync(hubMessage.GetSerializedMessage(protocol), cancellationToken);
    }

    public void Ack(AckMessage ackMessage)
    {
        var index = _index;
        for (var i = 0; i < _buffer.Length; i++)
        {
            var currentIndex = (index + i) % _buffer.Length;
            if (_buffer[currentIndex].SequenceId is long id && id <= ackMessage.SequenceId)
            {
                _buffer[currentIndex] = (null, null);
            }
        }

        // Release backpressure?
    }
}

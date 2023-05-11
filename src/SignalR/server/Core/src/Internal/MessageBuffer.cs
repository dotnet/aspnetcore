// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal sealed class MessageBuffer
{
    private readonly SerializedHubMessage[] _buffer;
    private int _index;

    // TODO: pass in limits
    public MessageBuffer()
    {
        _buffer = new SerializedHubMessage[10];
    }

    public async ValueTask WriteAsync(PipeWriter pipeWriter, SerializedHubMessage hubMessage, IHubProtocol protocol,
        CancellationToken cancellationToken)
    {
        // No lock because this is always called in a single async loop?
        // And other methods don't affect the checks here?

        // TODO: Backpressure

        if (_buffer[_index] is not null)
        {
            // ...
        }
        _buffer[_index] = hubMessage;
        _index = _index + 1 % _buffer.Length;
        await pipeWriter.WriteAsync(hubMessage.GetSerializedMessage(protocol), cancellationToken);
    }
}

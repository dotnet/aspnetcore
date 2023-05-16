// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal sealed class MessageBuffer
{
    private readonly (SerializedHubMessage? Message, long SequenceId)[] _buffer;
    private readonly ConnectionContext _connection;
    private readonly IHubProtocol _protocol;

    private int _index;
    private long _totalMessageCount;

    private TaskCompletionSource<FlushResult> _resend = new TaskCompletionSource<FlushResult>(TaskCreationOptions.RunContinuationsAsynchronously);

    // TODO: pass in limits
    public MessageBuffer(ConnectionContext connection, IHubProtocol protocol)
    {
        _buffer = new (SerializedHubMessage? Message, long SequenceId)[10];
        for (var i = 0; i < _buffer.Length; i++)
        {
            _buffer[i].SequenceId = long.MinValue;
        }
        _connection = connection;
        _protocol = protocol;

        _resend.SetResult(new());
    }

    public async ValueTask<FlushResult> WriteAsync(SerializedHubMessage hubMessage, IHubProtocol protocol,
        CancellationToken cancellationToken)
    {
        // No lock because this is always called in a single async loop?
        // And other methods don't affect the checks here?
        // Sending ping does hit this method, but it shouldn't modify any state

        // TODO: Backpressure

        if (_buffer[_index].Message is not null)
        {
            // ...
        }

        await _resend.Task.ConfigureAwait(false);

        try
        {

            if (hubMessage.Message is HubInvocationMessage invocationMessage)
            {
                _totalMessageCount++;
            }
            else
            {
                // Non-ackable message, don't add to buffer
                return await _connection.Transport.Output.WriteAsync(hubMessage.GetSerializedMessage(protocol), cancellationToken).ConfigureAwait(false);
            }

            _buffer[_index] = (hubMessage, _totalMessageCount);
            _index = (_index + 1) % _buffer.Length;
            return await _connection.Transport.Output.WriteAsync(hubMessage.GetSerializedMessage(protocol), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // TODO: specific exception or some identifier needed

            // wait for reconnect, send SequenceMessage, and then do resend loop

            var oldTcs = Interlocked.Exchange(ref _resend, new TaskCompletionSource<FlushResult>(TaskCreationOptions.RunContinuationsAsynchronously));
            if (!oldTcs.Task.IsCompleted)
            {
                return await oldTcs.Task.ConfigureAwait(false);
            }
            return await _resend.Task.ConfigureAwait(false);

            //long latestAckedIndex = -1;
            //for (var i = 0; i < _buffer.Length - 1; i++)
            //{
            //    if (_buffer[(_index + i + 1) % _buffer.Length].SequenceId > long.MinValue)
            //    {
            //        latestAckedIndex = (_index + i + 1) % _buffer.Length;
            //    }
            //}

            //if (latestAckedIndex == -1)
            //{
            //    // no unacked messages, probably not possible
            //    // because we are in the middle of writing a message when we get here, so there should be 1 minimum
            //}

            //protocol.WriteMessage(new SequenceMessage(_buffer[latestAckedIndex].SequenceId), connection.Transport.Output);
            //await connection.Transport.Output.FlushAsync(cancellationToken).ConfigureAwait(false);

            //for (var i = 0; i < _buffer.Length; i++)
            //{
            //    var item = _buffer[(latestAckedIndex + i) % _buffer.Length];
            //    if (item.SequenceId > long.MinValue)
            //    {
            //        await connection.Transport.Output.WriteAsync(item.Message!.GetSerializedMessage(protocol), cancellationToken).ConfigureAwait(false);
            //    }
            //    else
            //    {
            //        break;
            //    }
            //}
        }
    }

    public void Ack(AckMessage ackMessage)
    {
        var index = _index;
        for (var i = 0; i < _buffer.Length; i++)
        {
            var currentIndex = (index + i) % _buffer.Length;
            if (_buffer[currentIndex].SequenceId <= ackMessage.SequenceId)
            {
                _buffer[currentIndex] = (null, long.MinValue);
            }
        }

        // Release backpressure?
    }

    private long _currentReceivingSequenceId;
    private long _latestReceivedSequenceId = long.MinValue;

    internal bool ShouldProcessMessage(HubInvocationMessage message)
    {
        // TODO: if we're expecting a sequence message but get here we should error

        var currentId = _currentReceivingSequenceId;
        _currentReceivingSequenceId++;
        if (currentId <= _latestReceivedSequenceId)
        {
            // Ignore, this is a duplicate message
            return false;
        }
        _latestReceivedSequenceId = currentId;

        return true;
    }

    internal void ResetSequence(SequenceMessage sequenceMessage)
    {
        // TODO: is a sequence message expected right now?

        if (sequenceMessage.SequenceId > _currentReceivingSequenceId)
        {
            throw new Exception("Sequence ID greater than amount we've acked");
        }
        _currentReceivingSequenceId = sequenceMessage.SequenceId;
    }

    internal void Resend()
    {
        var tcs = new TaskCompletionSource<FlushResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldTcs = Interlocked.Exchange(ref _resend, tcs);
        if (!oldTcs.Task.IsCompleted)
        {
            Interlocked.Exchange(ref _resend, oldTcs);
            tcs = oldTcs;
        }
        _ = DoResendAsync(tcs);
    }

    private async Task DoResendAsync(TaskCompletionSource<FlushResult> tcs)
    {
        long latestAckedIndex = -1;
        for (var i = 0; i < _buffer.Length - 1; i++)
        {
            if (_buffer[(_index + i + 1) % _buffer.Length].SequenceId > long.MinValue)
            {
                latestAckedIndex = (_index + i + 1) % _buffer.Length;
                break;
            }
        }

        if (latestAckedIndex == -1)
        {
            // no unacked messages, probably not possible
            // because we are in the middle of writing a message when we get here, so there should be 1 minimum
        }

        FlushResult finalResult = new();
        try
        {
            _protocol.WriteMessage(new SequenceMessage(_buffer[latestAckedIndex].SequenceId), _connection.Transport.Output);
            finalResult = await _connection.Transport.Output.FlushAsync().ConfigureAwait(false);

            for (var i = 0; i < _buffer.Length; i++)
            {
                var item = _buffer[(latestAckedIndex + i) % _buffer.Length];
                if (item.SequenceId > long.MinValue)
                {
                    finalResult = await _connection.Transport.Output.WriteAsync(item.Message!.GetSerializedMessage(_protocol)).ConfigureAwait(false);
                }
                else
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }
        finally
        {
            tcs.TrySetResult(finalResult);
        }
    }
}

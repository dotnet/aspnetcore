// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal sealed class MessageBuffer : IDisposable
{
    private static readonly TaskCompletionSource<FlushResult> _completedTCS = new TaskCompletionSource<FlushResult>();

    private readonly (SerializedHubMessage? Message, long SequenceId)[] _buffer;
    private readonly ConnectionContext _connection;
    private readonly IHubProtocol _protocol;
    private readonly AckMessage _ackMessage = new(0);
    private readonly SequenceMessage _sequenceMessage = new(0);
    private readonly Channel<int> _waitForAck = Channel.CreateBounded<int>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest });

#if NET8_0_OR_GREATER
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(1));
#else
    private readonly TimerAwaitable _timer = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
#endif
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private int _bufferIndex;
    private long _totalMessageCount;
    private bool _waitForSequenceMessage;

    // Message IDs start at 1 and always increment by 1
    private long _currentReceivingSequenceId = 1;
    private long _latestReceivedSequenceId = long.MinValue;
    private long _lastAckedId = long.MinValue;

    private TaskCompletionSource<FlushResult> _resend = _completedTCS;

    static MessageBuffer()
    {
        _completedTCS.SetResult(new());
    }

    // TODO: pass in limits
    public MessageBuffer(ConnectionContext connection, IHubProtocol protocol)
    {
        // Arbitrary size, we can figure out defaults and configurability later
        const int bufferSize = 10;
        _buffer = new (SerializedHubMessage? Message, long SequenceId)[bufferSize];
        for (var i = 0; i < _buffer.Length; i++)
        {
            _buffer[i].SequenceId = long.MinValue;
        }
        _connection = connection;
        _protocol = protocol;

#if !NET8_0_OR_GREATER
        _timer.Start();
#endif
        _ = RunTimer();
    }

    private async Task RunTimer()
    {
        using (_timer)
        {
#if NET8_0_OR_GREATER
            while (await _timer.WaitForNextTickAsync().ConfigureAwait(false))
#else
            while (await _timer)
#endif
            {
                if (_lastAckedId < _latestReceivedSequenceId)
                {
                    // TODO: consider a minimum time between sending these?

                    var sequenceId = _latestReceivedSequenceId;

                    _ackMessage.SequenceId = sequenceId;

                    await _writeLock.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        _protocol.WriteMessage(_ackMessage, _connection.Transport.Output);
                        await _connection.Transport.Output.FlushAsync().ConfigureAwait(false);
                        _lastAckedId = sequenceId;
                    }
                    finally
                    {
                        _writeLock.Release();
                    }
                }
            }
        }
    }

    // TODO: WriteAsync(HubMessage) overload, so we don't allocate SerializedHubMessage for messages that aren't going to be buffered
    public async ValueTask<FlushResult> WriteAsync(SerializedHubMessage hubMessage, CancellationToken cancellationToken)
    {
        // TODO: Backpressure based on message count and total message size
        if (_buffer[_bufferIndex].Message is not null)
        {
            // primitive backpressure if buffer is full
            while (await _waitForAck.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_waitForAck.Reader.TryRead(out var index)
                    && (index == _bufferIndex || _buffer[_bufferIndex].Message is null))
                {
                    break;
                }
            }
        }

        // Avoid condition where last Ack position is the position we're currently writing into the buffer
        // If we wrote messages around the entire buffer before another Ack arrived we would end up reading the Ack position and writing over a buffered message
        _waitForAck.Reader.TryRead(out _);

        // TODO: We could consider buffering messages until they hit backpressure in the case when the connection is down
        await _resend.Task.ConfigureAwait(false);

        var waitForResend = false;

        await _writeLock.WaitAsync(cancellationToken: default).ConfigureAwait(false);
        try
        {
            if (hubMessage.Message is HubInvocationMessage invocationMessage)
            {
                _totalMessageCount++;
            }
            else
            {
                // Non-ackable message, don't add to buffer
                return await _connection.Transport.Output.WriteAsync(hubMessage.GetSerializedMessage(_protocol), cancellationToken).ConfigureAwait(false);
            }

            _buffer[_bufferIndex] = (hubMessage, _totalMessageCount);
            _bufferIndex = (_bufferIndex + 1) % _buffer.Length;

            return await _connection.Transport.Output.WriteAsync(hubMessage.GetSerializedMessage(_protocol), cancellationToken).ConfigureAwait(false);
        }
        // TODO: figure out what exception to use
        catch (ConnectionResetException)
        {
            waitForResend = true;
        }
        finally
        {
            _writeLock.Release();
        }

        if (waitForResend)
        {
            var oldTcs = Interlocked.Exchange(ref _resend, new TaskCompletionSource<FlushResult>(TaskCreationOptions.RunContinuationsAsynchronously));
            if (!oldTcs.Task.IsCompleted)
            {
                return await oldTcs.Task.ConfigureAwait(false);
            }
            return await _resend.Task.ConfigureAwait(false);
        }

        throw new NotImplementedException("shouldn't reach here");
    }

    public void Ack(AckMessage ackMessage)
    {
        // TODO: what if ackMessage.SequenceId is larger than last sent message?

        // Grabbing _bufferIndex unsynchronized should be fine, we might miss the most recent message but the client shouldn't be able to ack that yet
        // Or in exceptional cases we could miss multiple messages, but the next ack will clear them
        var index = _bufferIndex;
        var finalIndex = -1;
        for (var i = 0; i < _buffer.Length; i++)
        {
            var currentIndex = (index + i) % _buffer.Length;
            if (_buffer[currentIndex].Message is not null && _buffer[currentIndex].SequenceId <= ackMessage.SequenceId)
            {
                _buffer[currentIndex] = (null, long.MinValue);
                finalIndex = currentIndex;
            }
            // TODO: figure out an early exit?
        }

        // Release backpressure
        if (finalIndex > 0)
        {
            _waitForAck.Writer.TryWrite(finalIndex);
        }
    }

    internal bool ShouldProcessMessage(HubMessage message)
    {
        // TODO: if we're expecting a sequence message but get here should we error or ignore?
        if (_waitForSequenceMessage)
        {
            if (message is SequenceMessage)
            {
                _waitForSequenceMessage = false;
                return true;
            }
            else
            {
                // ignore messages received while waiting for sequence message
                return false;
            }
        }

        // Only care about messages implementing HubInvocationMessage currently (e.g. ignore ping, close, ack, sequence)
        // Could expand in the future, but should probably rev the ack version if changes are made
        if (message is not HubInvocationMessage)
        {
            return true;
        }

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
            throw new InvalidOperationException("Sequence ID greater than amount we've acked");
        }
        _currentReceivingSequenceId = sequenceMessage.SequenceId;
    }

    internal void Resend()
    {
        _waitForSequenceMessage = true;

        var tcs = new TaskCompletionSource<FlushResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldTcs = Interlocked.Exchange(ref _resend, tcs);
        // WriteAsync can also try to swap the TCS, we need to check if it's completed to know if it was swapped or not
        if (!oldTcs.Task.IsCompleted)
        {
            // Swap back to the TCS created by WriteAsync since it's waiting on the result of that task
            Interlocked.Exchange(ref _resend, oldTcs);
            tcs = oldTcs;
        }
        _ = DoResendAsync(tcs);
    }

    private async Task DoResendAsync(TaskCompletionSource<FlushResult> tcs)
    {
        FlushResult finalResult = new();
        await _writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            long latestAckedIndex = -1;
            for (var i = 0; i < _buffer.Length - 1; i++)
            {
                if (_buffer[(_bufferIndex + i + 1) % _buffer.Length].SequenceId > long.MinValue)
                {
                    latestAckedIndex = (_bufferIndex + i + 1) % _buffer.Length;
                    break;
                }
            }

            if (latestAckedIndex == -1)
            {
                // no unacked messages, still send SequenceMessage?
                return;
            }

            _sequenceMessage.SequenceId = _buffer[latestAckedIndex].SequenceId;
            _protocol.WriteMessage(_sequenceMessage, _connection.Transport.Output);
            // don't need to call flush just for the SequenceMessage if we're writing more messages
            var shouldFlush = true;

            for (var i = 0; i < _buffer.Length; i++)
            {
                var item = _buffer[(latestAckedIndex + i) % _buffer.Length];
                if (item.SequenceId > long.MinValue)
                {
                    finalResult = await _connection.Transport.Output.WriteAsync(item.Message!.GetSerializedMessage(_protocol)).ConfigureAwait(false);
                    shouldFlush = false;
                }
                else
                {
                    break;
                }
            }

            if (shouldFlush)
            {
                finalResult = await _connection.Transport.Output.FlushAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }
        finally
        {
            _writeLock.Release();
            tcs.TrySetResult(finalResult);
        }
    }

    public void Dispose()
    {
        ((IDisposable)_timer).Dispose();
    }
}

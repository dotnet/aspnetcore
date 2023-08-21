// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

    private readonly ConnectionContext _connection;
    private readonly IHubProtocol _protocol;
    private readonly long _bufferLimit;
    private readonly AckMessage _ackMessage = new(0);
    private readonly SequenceMessage _sequenceMessage = new(0);
    private readonly Channel<long> _waitForAck = Channel.CreateBounded<long>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest });

#if NET8_0_OR_GREATER
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(1));
#else
    private readonly TimerAwaitable _timer = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
#endif
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private long _totalMessageCount;
    private bool _waitForSequenceMessage;

    // Message IDs start at 1 and always increment by 1
    private long _currentReceivingSequenceId = 1;
    private long _latestReceivedSequenceId = long.MinValue;
    private long _lastAckedId = long.MinValue;

    private TaskCompletionSource<FlushResult> _resend = _completedTCS;

    private object Lock => _buffer;

    private LinkedBuffer _buffer;
    private long _bufferedByteCount;

    static MessageBuffer()
    {
        _completedTCS.SetResult(new());
    }

    public MessageBuffer(ConnectionContext connection, IHubProtocol protocol, long bufferLimit)
    {
        // TODO: pool
        _buffer = new LinkedBuffer();

        _connection = connection;
        _protocol = protocol;
        _bufferLimit = bufferLimit;

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

    /// <summary>
    /// Calling code is assumed to not call this method in parallel. Currently HubConnection and HubConnectionContext respect that.
    /// </summary>
    // TODO: WriteAsync(HubMessage) overload, so we don't allocate SerializedHubMessage for messages that aren't going to be buffered
    public async ValueTask<FlushResult> WriteAsync(SerializedHubMessage hubMessage, CancellationToken cancellationToken)
    {
        // TODO: Backpressure based on message count and total message size
        if (_bufferedByteCount > _bufferLimit)
        {
            // primitive backpressure if buffer is full
            while (await _waitForAck.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_waitForAck.Reader.TryRead(out var count) && count < _bufferLimit)
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

            var messageBytes = hubMessage.GetSerializedMessage(_protocol);
            lock (Lock)
            {
                _bufferedByteCount += messageBytes.Length;
                _buffer.AddMessage(hubMessage, _totalMessageCount);
            }

            return await _connection.Transport.Output.WriteAsync(messageBytes, cancellationToken).ConfigureAwait(false);
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

        var newCount = -1L;

        lock (Lock)
        {
            var item = _buffer.RemoveMessages(ackMessage.SequenceId, _protocol);
            _buffer = item.Item1;
            _bufferedByteCount -= item.Item2;

            newCount = _bufferedByteCount;
        }

        // Release potential backpressure
        if (newCount >= 0)
        {
            _waitForAck.Writer.TryWrite(newCount);
        }
    }

    internal bool ShouldProcessMessage(HubMessage message)
    {
        // TODO: if we're expecting a sequence message but get here should we error or ignore or maybe even continue to process them?
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
            throw new InvalidOperationException("Sequence ID greater than amount of messages we've received.");
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
            _sequenceMessage.SequenceId = _totalMessageCount + 1;

            var isFirst = true;
            foreach (var item in _buffer.GetMessages())
            {
                if (item.SequenceId > 0)
                {
                    if (isFirst)
                    {
                        _sequenceMessage.SequenceId = item.SequenceId;
                        _protocol.WriteMessage(_sequenceMessage, _connection.Transport.Output);
                        isFirst = false;
                    }
                    finalResult = await _connection.Transport.Output.WriteAsync(item.HubMessage!.GetSerializedMessage(_protocol)).ConfigureAwait(false);
                }
            }

            if (isFirst)
            {
                _protocol.WriteMessage(_sequenceMessage, _connection.Transport.Output);
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

    // Linked list of SerializedHubMessage arrays, sort of like ReadOnlySequence
    private sealed class LinkedBuffer
    {
        private const int BufferLength = 10;

        private int _currentIndex = -1;
        private int _ackedIndex = -1;
        private long _startingSequenceId = long.MinValue;
        private LinkedBuffer? _next;

        private readonly SerializedHubMessage?[] _messages = new SerializedHubMessage?[BufferLength];

        public void AddMessage(SerializedHubMessage hubMessage, long sequenceId)
        {
            if (_startingSequenceId < 0)
            {
                Debug.Assert(_currentIndex == -1);
                _startingSequenceId = sequenceId;
            }

            if (_currentIndex < BufferLength - 1)
            {
                Debug.Assert(_startingSequenceId + _currentIndex + 1 == sequenceId);

                _currentIndex++;
                _messages[_currentIndex] = hubMessage;
            }
            else if (_next is null)
            {
                _next = new LinkedBuffer();
                _next.AddMessage(hubMessage, sequenceId);
            }
            else
            {
                // TODO: Should we avoid this path by keeping a tail pointer?
                // Debug.Assert(false);

                var linkedBuffer = _next;
                while (linkedBuffer._next is not null)
                {
                    linkedBuffer = linkedBuffer._next;
                }

                // TODO: verify no stack overflow potential
                linkedBuffer.AddMessage(hubMessage, sequenceId);
            }
        }

        public (LinkedBuffer, int returnCredit) RemoveMessages(long sequenceId, IHubProtocol protocol)
        {
            return RemoveMessagesCore(this, sequenceId, protocol);
        }

        private static (LinkedBuffer, int returnCredit) RemoveMessagesCore(LinkedBuffer linkedBuffer, long sequenceId, IHubProtocol protocol)
        {
            var returnCredit = 0;
            while (linkedBuffer._startingSequenceId <= sequenceId)
            {
                var numElements = (int)Math.Min(BufferLength, Math.Max(1, sequenceId - (linkedBuffer._startingSequenceId - 1)));
                Debug.Assert(numElements > 0 && numElements < BufferLength + 1);

                for (var i = 0; i < numElements; i++)
                {
                    returnCredit += linkedBuffer._messages[i]?.GetSerializedMessage(protocol).Length ?? 0;
                    linkedBuffer._messages[i] = null;
                }

                linkedBuffer._ackedIndex = numElements - 1;

                if (numElements == BufferLength)
                {
                    if (linkedBuffer._next is null)
                    {
                        linkedBuffer.Reset(shouldPool: false);
                        return (linkedBuffer, returnCredit);
                    }
                    else
                    {
                        var tmp = linkedBuffer;
                        linkedBuffer = linkedBuffer._next;
                        tmp.Reset(shouldPool: true);
                    }
                }
                else
                {
                    return (linkedBuffer, returnCredit);
                }
            }

            return (linkedBuffer, returnCredit);
        }

        private void Reset(bool shouldPool)
        {
            _startingSequenceId = long.MinValue;
            _currentIndex = -1;
            _ackedIndex = -1;
            _next = null;

            Array.Clear(_messages, 0, BufferLength);

            // TODO: Add back to pool
            if (shouldPool)
            {
            }
        }

        public IEnumerable<(SerializedHubMessage? HubMessage, long SequenceId)> GetMessages()
        {
            return new Enumerable(this);
        }

        private struct Enumerable : IEnumerable<(SerializedHubMessage?, long)>
        {
            private readonly LinkedBuffer _linkedBuffer;

            public Enumerable(LinkedBuffer linkedBuffer)
            {
                _linkedBuffer = linkedBuffer;
            }

            public IEnumerator<(SerializedHubMessage?, long)> GetEnumerator()
            {
                return new Enumerator(_linkedBuffer);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        private struct Enumerator : IEnumerator<(SerializedHubMessage?, long)>
        {
            private LinkedBuffer? _linkedBuffer;
            private int _index;

            public Enumerator(LinkedBuffer linkedBuffer)
            {
                _linkedBuffer = linkedBuffer;
            }

            public (SerializedHubMessage?, long) Current
            {
                get
                {
                    if (_linkedBuffer is null)
                    {
                        return (null, long.MinValue);
                    }

                    var index = _index - 1;
                    var firstMessageIndex = _linkedBuffer._ackedIndex + 1;
                    if (firstMessageIndex + index < BufferLength)
                    {
                        return (_linkedBuffer._messages[firstMessageIndex + index], _linkedBuffer._startingSequenceId + firstMessageIndex + index);
                    }

                    return (null, long.MinValue);
                }
            }

            object IEnumerator.Current => throw new NotImplementedException();

            public void Dispose()
            {
                _linkedBuffer = null;
            }

            public bool MoveNext()
            {
                if (_linkedBuffer is null)
                {
                    return false;
                }

                var firstMessageIndex = _linkedBuffer._ackedIndex + 1;
                if (firstMessageIndex + _index >= BufferLength)
                {
                    _linkedBuffer = _linkedBuffer._next;
                    _index = 1;
                }
                else
                {
                    if (_linkedBuffer._messages[firstMessageIndex + _index] is null)
                    {
                        _linkedBuffer = null;
                    }
                    else
                    {
                        _index++;
                    }
                }

                return _linkedBuffer is not null;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }
}

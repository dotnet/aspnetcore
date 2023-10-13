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
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal sealed class MessageBuffer : IDisposable
{
    private static readonly TaskCompletionSource<FlushResult> _completedTCS = new TaskCompletionSource<FlushResult>();

    public static TimeSpan AckRate => TimeSpan.FromSeconds(1);

    private const int PoolLimit = 10;

    private readonly IHubProtocol _protocol;
    private readonly long _bufferLimit;
    private readonly ILogger _logger;
    private readonly AckMessage _ackMessage = new(0);
    private readonly SequenceMessage _sequenceMessage = new(0);
    private readonly Channel<long> _waitForAck = Channel.CreateBounded<long>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest });

#if NET8_0_OR_GREATER
    private readonly PeriodicTimer _timer;
#else
    private readonly TimerAwaitable _timer = new(AckRate, AckRate);
#endif
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private PipeWriter _writer;

    private long _totalMessageCount;

    // Message IDs start at 1 and always increment by 1
    private long _currentReceivingSequenceId = 1;
    private long _latestReceivedSequenceId = long.MinValue;
    private long _lastAckedId = long.MinValue;

    private TaskCompletionSource<FlushResult> _resend = _completedTCS;

    // Pool per connection
    private readonly Stack<LinkedBuffer> _pool = new();

    private LinkedBuffer _buffer;
    private long _bufferedByteCount;

    static MessageBuffer()
    {
        _completedTCS.SetResult(new());
    }

    public MessageBuffer(ConnectionContext connection, IHubProtocol protocol, long bufferLimit, ILogger logger)
        : this(connection, protocol, bufferLimit, logger, TimeProvider.System)
    {
    }

    public MessageBuffer(ConnectionContext connection, IHubProtocol protocol, long bufferLimit, ILogger logger, TimeProvider timeProvider)
    {
#if NET8_0_OR_GREATER
        timeProvider ??= TimeProvider.System;
        _timer = new(AckRate, timeProvider);
#endif

        _buffer = new LinkedBuffer();

        _writer = connection.Transport.Output;
        _protocol = protocol;
        _bufferLimit = bufferLimit;
        _logger = logger;

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
                        _protocol.WriteMessage(_ackMessage, _writer);
                        await _writer.FlushAsync().ConfigureAwait(false);
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

    public ValueTask<FlushResult> WriteAsync(SerializedHubMessage hubMessage, CancellationToken cancellationToken)
    {
        return WriteAsyncCore(hubMessage.Message!, hubMessage.GetSerializedMessage(_protocol), cancellationToken);
    }

    public ValueTask<FlushResult> WriteAsync(HubMessage hubMessage, CancellationToken cancellationToken)
    {
        return WriteAsyncCore(hubMessage, _protocol.GetMessageBytes(hubMessage), cancellationToken);
    }

    private async ValueTask<FlushResult> WriteAsyncCore(HubMessage hubMessage, ReadOnlyMemory<byte> messageBytes, CancellationToken cancellationToken)
    {
        // TODO: Add backpressure based on message count
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

        // Await the flush outside of the _writeLock so AckAsync can make forward progress if there is backpressure.
        // Multiple flush calls is fine, it's multiple GetMemory calls that are problematic, which is fine since we lock around the actual write
        ValueTask<FlushResult> writeTask;

        await _writeLock.WaitAsync(cancellationToken: default).ConfigureAwait(false);
        try
        {
            if (hubMessage is HubInvocationMessage invocationMessage)
            {
                _totalMessageCount++;
                _bufferedByteCount += messageBytes.Length;
                _buffer.AddMessage(messageBytes, _totalMessageCount, _pool);

                writeTask = _writer.WriteAsync(messageBytes, cancellationToken);
            }
            else
            {
                // Non-ackable message, don't add to buffer
                writeTask = _writer.WriteAsync(messageBytes, cancellationToken);
            }
        }
        finally
        {
            _writeLock.Release();
        }

        return await writeTask.ConfigureAwait(false);
    }

    public async Task AckAsync(AckMessage ackMessage)
    {
        // TODO: what if ackMessage.SequenceId is larger than last sent message?

        var newCount = -1L;

        await _writeLock.WaitAsync(cancellationToken: default).ConfigureAwait(false);
        try
        {
            var item = _buffer.RemoveMessages(ackMessage.SequenceId, _pool);
            _buffer = item.Buffer;
            _bufferedByteCount -= item.ReturnCredit;

            newCount = _bufferedByteCount;
        }
        finally
        {
            _writeLock.Release();
        }

        // Release potential backpressure
        if (newCount >= 0)
        {
            _waitForAck.Writer.TryWrite(newCount);
        }
    }

    internal bool ShouldProcessMessage(HubMessage message)
    {
        if (message is SequenceMessage sequenceMessage)
        {
            // TODO: is a sequence message expected right now?

            if (sequenceMessage.SequenceId > _currentReceivingSequenceId)
            {
                throw new InvalidOperationException("Sequence ID greater than amount of messages we've received.");
            }

            _currentReceivingSequenceId = sequenceMessage.SequenceId;

            // Technically handled by the 'is not HubInvocationMessage' check, but this is future proofing in case that check changes
            // SequenceMessage should not be counted towards ackable messages
            return true;
        }

        // Only care about messages implementing HubInvocationMessage currently (e.g. ignore ping, close, ack, sequence)
        // Could expand in the future, but should probably rev the ack version if changes are made
        if (message is not HubInvocationMessage)
        {
            return true;
        }

        var currentId = _currentReceivingSequenceId;
        // ShouldProcessMessage is never called in parallel and is the only method referencing _currentReceivingSequenceId
        _currentReceivingSequenceId++;
        if (currentId <= _latestReceivedSequenceId)
        {
            // Ignore, this is a duplicate message
            return false;
        }
        _latestReceivedSequenceId = currentId;

        return true;
    }

    internal async Task ResendAsync(PipeWriter writer)
    {
        var tcs = new TaskCompletionSource<FlushResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _resend = tcs;

        FlushResult finalResult = new();
        await _writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Complete previous pipe so transport reader can cleanup
            _writer.Complete();
            // Replace writer with new pipe that the transport will be reading from
            _writer = writer;

            _sequenceMessage.SequenceId = _totalMessageCount + 1;

            var isFirst = true;
            // Loop over all buffered messages and send them
            foreach (var item in _buffer.GetMessages())
            {
                if (item.SequenceId > 0)
                {
                    // The first message we send is a SequenceMessage with the ID of the first unacked message we're sending
                    if (isFirst)
                    {
                        _sequenceMessage.SequenceId = item.SequenceId;
                        // No need to flush since we're immediately calling WriteAsync after
                        _protocol.WriteMessage(_sequenceMessage, _writer);
                        isFirst = false;
                    }
                    // Use WriteAsync instead of doing all Writes and then a FlushAsync so we can observe backpressure
                    finalResult = await _writer.WriteAsync(item.HubMessage).ConfigureAwait(false);
                }
            }

            // There were no buffered messages, we still need to send the SequenceMessage to let the client know what ID messages will start at
            if (isFirst)
            {
                _protocol.WriteMessage(_sequenceMessage, _writer);
                finalResult = await _writer.FlushAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
            // Observe exception in case WriteAsync isn't waiting on the task
            _ = tcs.Task.Exception;
            _logger.LogDebug(ex, "Failure while resending messages after a reconnect.");
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
    // Not a thread safe class, should be used with locking in mind
    private sealed class LinkedBuffer
    {
        private const int BufferLength = 10;

        private int _currentIndex = -1;
        private int _ackedIndex = -1;
        private long _startingSequenceId = long.MinValue;
        private LinkedBuffer? _next;

        private readonly ReadOnlyMemory<byte>[] _messages = new ReadOnlyMemory<byte>[BufferLength];

        public void AddMessage(ReadOnlyMemory<byte> hubMessage, long sequenceId, Stack<LinkedBuffer> pool)
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
                if (pool.Count != 0)
                {
                    _next = pool.Pop();
                }
                else
                {
                    _next = new LinkedBuffer();
                }
                _next.AddMessage(hubMessage, sequenceId, pool);
            }
            else
            {
                // TODO: Should we avoid this path by keeping a tail pointer?

                var linkedBuffer = _next;
                while (linkedBuffer._next is not null)
                {
                    linkedBuffer = linkedBuffer._next;
                }

                linkedBuffer.AddMessage(hubMessage, sequenceId, pool);
            }
        }

        public (LinkedBuffer Buffer, int ReturnCredit) RemoveMessages(long sequenceId, Stack<LinkedBuffer> pool)
        {
            return RemoveMessagesCore(this, sequenceId, pool);
        }

        private static (LinkedBuffer Buffer, int ReturnCredit) RemoveMessagesCore(LinkedBuffer linkedBuffer, long sequenceId, Stack<LinkedBuffer> pool)
        {
            var returnCredit = 0;
            while (linkedBuffer._startingSequenceId <= sequenceId)
            {
                var numElements = (int)Math.Min(BufferLength, Math.Max(1, sequenceId - (linkedBuffer._startingSequenceId - 1)));
                Debug.Assert(numElements > 0 && numElements < BufferLength + 1);

                for (var i = 0; i < numElements; i++)
                {
                    returnCredit += linkedBuffer._messages[i].Length;
                    linkedBuffer._messages[i] = null;
                }

                linkedBuffer._ackedIndex = numElements - 1;

                if (numElements == BufferLength)
                {
                    if (linkedBuffer._next is null)
                    {
                        linkedBuffer.Reset();
                        return (linkedBuffer, returnCredit);
                    }
                    else
                    {
                        var tmp = linkedBuffer;
                        linkedBuffer = linkedBuffer._next;
                        tmp.Reset();
                        if (pool.Count < PoolLimit)
                        {
                            pool.Push(tmp);
                        }
                    }
                }
                else
                {
                    return (linkedBuffer, returnCredit);
                }
            }

            return (linkedBuffer, returnCredit);
        }

        private void Reset()
        {
            _startingSequenceId = long.MinValue;
            _currentIndex = -1;
            _ackedIndex = -1;
            _next = null;

            Array.Clear(_messages, 0, BufferLength);
        }

        public IEnumerable<(ReadOnlyMemory<byte> HubMessage, long SequenceId)> GetMessages()
        {
            return new Enumerable(this);
        }

        private struct Enumerable : IEnumerable<(ReadOnlyMemory<byte>, long)>
        {
            private readonly LinkedBuffer _linkedBuffer;

            public Enumerable(LinkedBuffer linkedBuffer)
            {
                _linkedBuffer = linkedBuffer;
            }

            public IEnumerator<(ReadOnlyMemory<byte>, long)> GetEnumerator()
            {
                return new Enumerator(_linkedBuffer);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        private struct Enumerator : IEnumerator<(ReadOnlyMemory<byte>, long)>
        {
            private LinkedBuffer? _linkedBuffer;
            private int _index;

            public Enumerator(LinkedBuffer linkedBuffer)
            {
                _linkedBuffer = linkedBuffer;
            }

            public (ReadOnlyMemory<byte>, long) Current
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
                    if (_linkedBuffer._messages[firstMessageIndex + _index].Length == 0)
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

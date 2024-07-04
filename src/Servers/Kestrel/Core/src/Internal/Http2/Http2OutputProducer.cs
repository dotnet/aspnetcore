// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

internal sealed class Http2OutputProducer : IHttpOutputProducer, IHttpOutputAborter, IDisposable
{
    private int StreamId => _stream.StreamId;
    private readonly Http2FrameWriter _frameWriter;
    private readonly TimingPipeFlusher _flusher;
    private readonly KestrelTrace _log;

    private readonly MemoryPool<byte> _memoryPool;
    private readonly Http2Stream _stream;
    private readonly object _dataWriterLock = new object();
    private readonly Pipe _pipe;
    private readonly ConcurrentPipeWriter _pipeWriter;
    private readonly PipeReader _pipeReader;
    private IMemoryOwner<byte>? _fakeMemoryOwner;
    private byte[]? _fakeMemory;
    private bool _startedWritingDataFrames;
    private bool _completeScheduled;
    private bool _suffixSent;
    private bool _appCompletedWithNoResponseBodyOrTrailers;
    private bool _writerComplete;
    private bool _isScheduled;

    // Internal for testing
    internal bool _disposed;

    private long _unconsumedBytes;
    private long _streamWindow;

    // For scheduling changes that don't affect the number of bytes written to the pipe, we need another state.
    private State _unobservedState;

    // This reflects the current state of the output, the current state becomes the unobserved state after it has been observed.
    private State _currentState;
    private bool _completedResponse;
    private bool _requestProcessingComplete;
    private bool _waitingForWindowUpdates;
    private Http2ErrorCode? _resetErrorCode;

    public Http2OutputProducer(Http2Stream stream, Http2StreamContext context)
    {
        _stream = stream;
        _frameWriter = context.FrameWriter;
        _memoryPool = context.MemoryPool;
        _log = context.ServiceContext.Log;
        var scheduleInline = context.ServiceContext.Scheduler == PipeScheduler.Inline;

        _pipe = CreateDataPipe(_memoryPool, scheduleInline);

        _pipeWriter = new ConcurrentPipeWriter(_pipe.Writer, _memoryPool, _dataWriterLock);
        _pipeReader = _pipe.Reader;

        // No need to pass in timeoutControl here, since no minDataRates are passed to the TimingPipeFlusher.
        // The minimum output data rate is enforced at the connection level by Http2FrameWriter.
        _flusher = new TimingPipeFlusher(timeoutControl: null, _log);
        _flusher.Initialize(_pipeWriter);
        _streamWindow = context.ClientPeerSettings.InitialWindowSize;
    }

    public Http2Stream Stream => _stream;
    public PipeReader PipeReader => _pipeReader;

    public bool IsTimingWrite { get; set; }

    public bool AppCompletedWithNoResponseBodyOrTrailers => _appCompletedWithNoResponseBodyOrTrailers;

    public bool CompletedResponse
    {
        get
        {
            lock (_dataWriterLock)
            {
                return _completedResponse;
            }
        }
    }

    // Useful for debugging the scheduling state in the debugger
    internal (int, long, State, State, long) SchedulingState => (Stream.StreamId, _unconsumedBytes, _unobservedState, _currentState, _streamWindow);

    public State UnobservedState
    {
        get
        {
            lock (_dataWriterLock)
            {
                return _unobservedState;
            }
        }
    }

    public State CurrentState
    {
        get
        {
            lock (_dataWriterLock)
            {
                return _currentState;
            }
        }
    }

    // Added bytes to the queue.
    // Returns a bool that represents whether we should schedule this producer to write
    // the enqueued bytes
    private void EnqueueDataWrite(long bytes)
    {
        lock (_dataWriterLock)
        {
            _unconsumedBytes += bytes;
        }
    }

    // Determines if we should schedule this producer to observe
    // any state changes made.
    private void EnqueueStateUpdate(State state)
    {
        lock (_dataWriterLock)
        {
            _unobservedState |= state;
        }
    }

    public void SetWaitingForWindowUpdates()
    {
        lock (_dataWriterLock)
        {
            _waitingForWindowUpdates = true;
        }
    }

    // Removes consumed bytes from the queue.
    // Returns a bool that represents whether we should schedule this producer to write
    // the remaining bytes.
    internal (bool hasMoreData, bool reschedule, State currentState, bool waitingForWindowUpdates) ObserveDataAndState(long bytes, State state)
    {
        lock (_dataWriterLock)
        {
            _isScheduled = false;
            _unobservedState &= ~state;
            _currentState |= state;
            _unconsumedBytes -= bytes;
            return (_unconsumedBytes > 0, _unobservedState != State.None, _currentState, _waitingForWindowUpdates);
        }
    }

    internal long CheckStreamWindow(long bytes)
    {
        lock (_dataWriterLock)
        {
            return Math.Min(bytes, _streamWindow);
        }
    }

    internal void ConsumeStreamWindow(long bytes)
    {
        lock (_dataWriterLock)
        {
            _streamWindow -= bytes;
        }
    }

    public void StreamReset(uint initialWindowSize)
    {
        // Response should have been completed.
        Debug.Assert(_completedResponse);

        _appCompletedWithNoResponseBodyOrTrailers = false;
        _suffixSent = false;
        _startedWritingDataFrames = false;
        _completeScheduled = false;
        _writerComplete = false;
        _pipe.Reset();
        _pipeWriter.Reset();

        _streamWindow = initialWindowSize;
        _unconsumedBytes = 0;
        _unobservedState = State.None;
        _currentState = State.None;
        _completedResponse = false;
        _requestProcessingComplete = false;
        _waitingForWindowUpdates = false;
        _resetErrorCode = null;
        IsTimingWrite = false;
    }

    public void Complete()
    {
        lock (_dataWriterLock)
        {
            if (_writerComplete)
            {
                return;
            }

            _writerComplete = true;

            Stop();

            if (!_completeScheduled)
            {
                EnqueueStateUpdate(State.Completed);

                // Make sure the writing side is completed.
                _pipeWriter.Complete();

                Schedule();
            }
            else
            {
                // Make sure the writing side is completed.
                _pipeWriter.Complete();
            }

            if (_fakeMemoryOwner != null)
            {
                _fakeMemoryOwner.Dispose();
                _fakeMemoryOwner = null;
            }

            if (_fakeMemory != null)
            {
                ArrayPool<byte>.Shared.Return(_fakeMemory);
                _fakeMemory = null;
            }
        }
    }

    // This is called when a CancellationToken fires mid-write.
    // In HTTP/1.x, this aborts the entire connection. For HTTP/2 we abort the stream.
    void IHttpOutputAborter.Abort(ConnectionAbortedException abortReason, ConnectionEndReason reason)
    {
        _stream.ResetAndAbort(abortReason, Http2ErrorCode.INTERNAL_ERROR);
    }

    void IHttpOutputAborter.OnInputOrOutputCompleted()
    {
        _stream.ResetAndAbort(new ConnectionAbortedException($"{nameof(Http2OutputProducer)} has completed."), Http2ErrorCode.INTERNAL_ERROR);
    }

    public ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new ValueTask<FlushResult>(Task.FromCanceled<FlushResult>(cancellationToken));
        }

        lock (_dataWriterLock)
        {
            ThrowIfSuffixSentOrCompleted();

            if (_completeScheduled)
            {
                return new ValueTask<FlushResult>(new FlushResult(false, true));
            }

            if (_startedWritingDataFrames)
            {
                // If there's already been response data written to the stream, just wait for that. Any header
                // should be in front of the data frames in the connection pipe. Trailers could change things.
                var task = _flusher.FlushAsync(this, cancellationToken);

                Schedule();

                return task;
            }
            else
            {
                Schedule();

                return default;
            }
        }
    }

    public void Schedule()
    {
        lock (_dataWriterLock)
        {
            // Lock here
            if (_isScheduled)
            {
                return;
            }

            _isScheduled = true;
        }

        _frameWriter.Schedule(this);
    }

    public bool TryScheduleNextWriteIfStreamWindowHasSpace()
    {
        lock (_dataWriterLock)
        {
            Debug.Assert(_unconsumedBytes > 0);

            // Check the stream window under the lock so that we don't miss window updates
            if (_streamWindow > 0)
            {
                Schedule();

                return true;
            }

            _waitingForWindowUpdates = true;
        }
        return false;
    }

    public void ScheduleResumeFromWindowUpdate()
    {
        if (_completedResponse)
        {
            return;
        }

        lock (_dataWriterLock)
        {
            _waitingForWindowUpdates = false;
        }

        Schedule();
    }

    public ValueTask<FlushResult> Write100ContinueAsync()
    {
        lock (_dataWriterLock)
        {
            ThrowIfSuffixSentOrCompleted();

            if (_completeScheduled)
            {
                return default;
            }

            return _frameWriter.Write100ContinueAsync(StreamId);
        }
    }

    public void WriteResponseHeaders(int statusCode, string? reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, bool appCompleted)
    {
        lock (_dataWriterLock)
        {
            // The HPACK header compressor is stateful, if we compress headers for an aborted stream we must send them.
            // Optimize for not compressing or sending them.
            if (_completeScheduled)
            {
                return;
            }

            // If the responseHeaders will be written as the final HEADERS frame then
            // set END_STREAM on the HEADERS frame. This avoids the need to write an
            // empty DATA frame with END_STREAM.
            //
            // The headers will be the final frame if:
            // 1. There is no content
            // 2. There is no trailing HEADERS frame.
            if (appCompleted && !_startedWritingDataFrames && (_stream.ResponseTrailers == null || _stream.ResponseTrailers.Count == 0))
            {
                _appCompletedWithNoResponseBodyOrTrailers = true;
            }

            EnqueueStateUpdate(State.FlushHeaders);
        }
    }

    public Task WriteDataAsync(ReadOnlySpan<byte> data, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_dataWriterLock)
        {
            ThrowIfSuffixSentOrCompleted();

            // This length check is important because we don't want to set _startedWritingDataFrames unless a data
            // frame will actually be written causing the headers to be flushed.
            if (_completeScheduled || data.Length == 0)
            {
                return Task.CompletedTask;
            }

            _startedWritingDataFrames = true;

            _pipeWriter.Write(data);

            EnqueueDataWrite(data.Length);

            var task = _flusher.FlushAsync(this, cancellationToken).GetAsTask();

            Schedule();

            return task;
        }
    }

    public ValueTask<FlushResult> WriteStreamSuffixAsync()
    {
        lock (_dataWriterLock)
        {
            if (_completeScheduled)
            {
                return ValueTask.FromResult<FlushResult>(default);
            }

            _completeScheduled = true;
            _suffixSent = true;

            EnqueueStateUpdate(State.Completed);

            _pipeWriter.Complete();

            Schedule();

            return ValueTask.FromResult<FlushResult>(default);
        }
    }

    public ValueTask<FlushResult> WriteRstStreamAsync(Http2ErrorCode error)
    {
        lock (_dataWriterLock)
        {
            // Stop() always schedules a completion if one wasn't scheduled already.
            Stop();
            // We queued the stream to complete but didn't complete the response yet
            if (!_completedResponse)
            {
                // Set the error so that we can write the RST when the response completes.
                _resetErrorCode = error;
                return default;
            }

            return _frameWriter.WriteRstStreamAsync(StreamId, error);
        }
    }

    public void Advance(int bytes)
    {
        lock (_dataWriterLock)
        {
            ThrowIfSuffixSentOrCompleted();

            if (_completeScheduled)
            {
                return;
            }

            _startedWritingDataFrames = true;

            _pipeWriter.Advance(bytes);

            EnqueueDataWrite(bytes);
        }
    }

    public long UnflushedBytes => _pipeWriter.UnflushedBytes;

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        lock (_dataWriterLock)
        {
            ThrowIfSuffixSentOrCompleted();

            if (_completeScheduled)
            {
                return GetFakeMemory(sizeHint).Span;
            }

            return _pipeWriter.GetSpan(sizeHint);
        }
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        lock (_dataWriterLock)
        {
            ThrowIfSuffixSentOrCompleted();

            if (_completeScheduled)
            {
                return GetFakeMemory(sizeHint);
            }

            return _pipeWriter.GetMemory(sizeHint);
        }
    }

    public void CancelPendingFlush()
    {
        lock (_dataWriterLock)
        {
            if (_completeScheduled)
            {
                return;
            }

            _pipeWriter.CancelPendingFlush();
        }
    }

    public ValueTask<FlushResult> WriteDataToPipeAsync(ReadOnlySpan<byte> data, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new ValueTask<FlushResult>(Task.FromCanceled<FlushResult>(cancellationToken));
        }

        lock (_dataWriterLock)
        {
            ThrowIfSuffixSentOrCompleted();

            // This length check is important because we don't want to set _startedWritingDataFrames unless a data
            // frame will actually be written causing the headers to be flushed.
            if (_completeScheduled || data.Length == 0)
            {
                return new ValueTask<FlushResult>(new FlushResult(false, true));
            }

            _startedWritingDataFrames = true;

            _pipeWriter.Write(data);

            EnqueueDataWrite(data.Length);
            var task = _flusher.FlushAsync(this, cancellationToken);

            Schedule();

            return task;
        }
    }

    public ValueTask<FlushResult> FirstWriteAsync(int statusCode, string? reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, ReadOnlySpan<byte> data, CancellationToken cancellationToken)
    {
        lock (_dataWriterLock)
        {
            WriteResponseHeaders(statusCode, reasonPhrase, responseHeaders, autoChunk, appCompleted: false);

            return WriteDataToPipeAsync(data, cancellationToken);
        }
    }

    ValueTask<FlushResult> IHttpOutputProducer.WriteChunkAsync(ReadOnlySpan<byte> data, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<FlushResult> FirstWriteChunkedAsync(int statusCode, string? reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, ReadOnlySpan<byte> data, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public void Stop()
    {
        lock (_dataWriterLock)
        {
            _waitingForWindowUpdates = false;

            if (_completeScheduled && _completedResponse)
            {
                // We can overschedule as long as we haven't yet completed the response. This is important because
                // we may need to abort the stream if it's waiting for a window update.
                return;
            }

            _completeScheduled = true;

            EnqueueStateUpdate(State.Aborted);

            Schedule();
        }
    }

    public void Reset()
    {
    }

    internal void OnRequestProcessingEnded()
    {
        var shouldCompleteStream = false;
        lock (_dataWriterLock)
        {
            if (_requestProcessingComplete)
            {
                // Noop, we're done
                return;
            }

            _requestProcessingComplete = true;

            shouldCompleteStream = _completedResponse;
        }

        // Complete outside of lock, anything this method does that needs a lock will acquire a lock itself.
        // Additionally, this method should only be called once per Reset so calling outside of the lock is fine from the perspective
        // of multiple threads calling OnRequestProcessingEnded.
        if (shouldCompleteStream)
        {
            Stream.CompleteStream(errored: false);
        }

    }

    internal ValueTask<FlushResult> CompleteResponseAsync()
    {
        var shouldCompleteStream = false;
        ValueTask<FlushResult> task = default;

        lock (_dataWriterLock)
        {
            if (_completedResponse)
            {
                // This should never be called twice
                return default;
            }

            _completedResponse = true;

            if (_resetErrorCode is { } error)
            {
                // If we have an error code to write, write it now that we're done with the response.
                // Always send the reset even if the response body is completed. The request body may not have completed yet.
                task = _frameWriter.WriteRstStreamAsync(StreamId, error);
            }

            shouldCompleteStream = _requestProcessingComplete;
        }

        // Complete outside of lock, anything this method does that needs a lock will acquire a lock itself.
        // CompleteResponseAsync also should never be called in parallel so calling this outside of the lock doesn't
        // cause any weirdness with parallel threads calling this method and no longer waiting on the stream completion call.
        if (shouldCompleteStream)
        {
            Stream.CompleteStream(errored: false);
        }

        return task;
    }

    internal Memory<byte> GetFakeMemory(int minSize)
    {
        // Try to reuse _fakeMemoryOwner
        if (_fakeMemoryOwner != null)
        {
            if (_fakeMemoryOwner.Memory.Length < minSize)
            {
                _fakeMemoryOwner.Dispose();
                _fakeMemoryOwner = null;
            }
            else
            {
                return _fakeMemoryOwner.Memory;
            }
        }

        // Try to reuse _fakeMemory
        if (_fakeMemory != null)
        {
            if (_fakeMemory.Length < minSize)
            {
                ArrayPool<byte>.Shared.Return(_fakeMemory);
                _fakeMemory = null;
            }
            else
            {
                return _fakeMemory;
            }
        }

        // Requesting a bigger buffer could throw.
        if (minSize <= _memoryPool.MaxBufferSize)
        {
            // Use the specified pool as it fits.
            _fakeMemoryOwner = _memoryPool.Rent(minSize);
            return _fakeMemoryOwner.Memory;
        }
        else
        {
            // Use the array pool. Its MaxBufferSize is int.MaxValue.
            return _fakeMemory = ArrayPool<byte>.Shared.Rent(minSize);
        }
    }

    public bool TryUpdateStreamWindow(int bytes)
    {
        var schedule = false;

        lock (_dataWriterLock)
        {
            var maxUpdate = Http2PeerSettings.MaxWindowSize - _streamWindow;

            if (bytes > maxUpdate)
            {
                return false;
            }

            schedule = UpdateStreamWindow(bytes);
        }

        if (schedule)
        {
            ScheduleResumeFromWindowUpdate();
        }

        return true;

        // Adds more bytes to the stream's window
        // Returns a bool that represents whether we should schedule this producer to write
        // the remaining bytes.
        bool UpdateStreamWindow(long bytes)
        {
            var wasDepleted = _streamWindow <= 0;
            _streamWindow += bytes;
            return wasDepleted && _streamWindow > 0 && _unconsumedBytes > 0;
        }
    }

    [StackTraceHidden]
    private void ThrowIfSuffixSentOrCompleted()
    {
        if (_suffixSent)
        {
            ThrowSuffixSent();
        }

        if (_writerComplete)
        {
            ThrowWriterComplete();
        }
    }

    [StackTraceHidden]
    private static void ThrowSuffixSent()
    {
        throw new InvalidOperationException("Writing is not allowed after writer was completed.");
    }

    [StackTraceHidden]
    private static void ThrowWriterComplete()
    {
        throw new InvalidOperationException("Cannot write to response after the request has completed.");
    }

    private static Pipe CreateDataPipe(MemoryPool<byte> pool, bool scheduleInline)
        => new Pipe(new PipeOptions
        (
            pool: pool,
            readerScheduler: PipeScheduler.Inline,
            writerScheduler: PipeScheduler.ThreadPool,
            // The unit tests rely on inline scheduling and the ability to control individual writes
            // and assert individual frames. Setting the thresholds to 1 avoids frames being coaleased together
            // and allows the test to assert them individually.
            pauseWriterThreshold: scheduleInline ? 1 : 4096,
            resumeWriterThreshold: scheduleInline ? 1 : 2048,
            useSynchronizationContext: false,
            minimumSegmentSize: pool.GetMinimumSegmentSize()
        ));

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
    }

    [Flags]
    public enum State
    {
        None = 0,
        FlushHeaders = 1,
        Aborted = 2,
        Completed = 4
    }
}

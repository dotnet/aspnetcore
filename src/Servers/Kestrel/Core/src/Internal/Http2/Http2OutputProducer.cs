// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading.Tasks.Sources;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

internal class Http2OutputProducer : IHttpOutputProducer, IHttpOutputAborter, IValueTaskSource<FlushResult>, IDisposable
{
    private int StreamId => _stream.StreamId;
    private readonly Http2FrameWriter _frameWriter;
    private readonly TimingPipeFlusher _flusher;
    private readonly KestrelTrace _log;

    // This should only be accessed via the FrameWriter. The connection-level output flow control is protected by the
    // FrameWriter's connection-level write lock.
    private readonly StreamOutputFlowControl _flowControl;
    private readonly MemoryPool<byte> _memoryPool;
    private readonly Http2Stream _stream;
    private readonly object _dataWriterLock = new object();
    private readonly Pipe _pipe;
    private readonly ConcurrentPipeWriter _pipeWriter;
    private readonly PipeReader _pipeReader;
    private IMemoryOwner<byte>? _fakeMemoryOwner;
    private byte[]? _fakeMemory;
    private bool _startedWritingDataFrames;
    private bool _streamCompleted;
    private bool _suffixSent;
    private bool _streamEnded;
    private bool _writerComplete;

    // Internal for testing
    internal bool _disposed;

    private long _unconsumedBytes;
    private long _window;

    // For changes scheduling changes that don't affect the number of bytes written to the pipe, we need another state
    private State _observationState;
    private bool _waitingForWindowUpdates;

    /// <summary>The core logic for the IValueTaskSource implementation.</summary>
    private ManualResetValueTaskSourceCore<FlushResult> _responseCompleteTaskSource = new ManualResetValueTaskSourceCore<FlushResult> { RunContinuationsAsynchronously = true }; // mutable struct, do not make this readonly

    // This object is itself usable as a backing source for ValueTask.  Since there's only ever one awaiter
    // for this object's state transitions at a time, we allow the object to be awaited directly. All functionality
    // associated with the implementation is just delegated to the ManualResetValueTaskSourceCore.
    private ValueTask<FlushResult> GetWaiterTask() => new ValueTask<FlushResult>(this, _responseCompleteTaskSource.Version);
    ValueTaskSourceStatus IValueTaskSource<FlushResult>.GetStatus(short token) => _responseCompleteTaskSource.GetStatus(token);
    void IValueTaskSource<FlushResult>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _responseCompleteTaskSource.OnCompleted(continuation, state, token, flags);
    FlushResult IValueTaskSource<FlushResult>.GetResult(short token) => _responseCompleteTaskSource.GetResult(token);

    public Http2OutputProducer(Http2Stream stream, Http2StreamContext context, StreamOutputFlowControl flowControl)
    {
        _stream = stream;
        _frameWriter = context.FrameWriter;
        _flowControl = flowControl;
        _memoryPool = context.MemoryPool;
        _log = context.ServiceContext.Log;

        _pipe = CreateDataPipe(_memoryPool);

        _pipeWriter = new ConcurrentPipeWriter(_pipe.Writer, _memoryPool, _dataWriterLock);
        Debug.Assert(_pipeWriter.CanGetUnflushedBytes);
        _pipeReader = _pipe.Reader;

        // No need to pass in timeoutControl here, since no minDataRates are passed to the TimingPipeFlusher.
        // The minimum output data rate is enforced at the connection level by Http2FrameWriter.
        _flusher = new TimingPipeFlusher(timeoutControl: null, _log);
        _flusher.Initialize(_pipeWriter);
        _window = context.ClientPeerSettings.InitialWindowSize;
    }

    public Http2Stream Stream => _stream;
    public PipeReader PipeReader => _pipeReader;

    public bool IsTimingWrite { get; set; }

    public bool WriteHeaders { get; set; }

    public bool StreamEnded => _streamEnded;

    public bool StreamCompleted
    {
        get
        {
            lock (_dataWriterLock)
            {
                return _streamCompleted;
            }
        }
    }

    // Useful for debugging the scheduling state in the debugger
    internal (int, long, State, long, bool) SchedulingState => (Stream.StreamId, _unconsumedBytes, _observationState, _window, _waitingForWindowUpdates);

    // Added bytes to the queue.
    // Returns a bool that represents whether we should schedule this producer to write
    // the enqueued bytes
    private bool Enqueue(long bytes)
    {
        lock (_dataWriterLock)
        {
            var wasEmpty = _unconsumedBytes == 0;
            _unconsumedBytes += bytes;
            return wasEmpty && _unconsumedBytes > 0 && _observationState == State.None;
        }
    }

    // Determines if we should schedule this producer to observe
    // any state changes made.
    private bool EnqueueForObservation(State state)
    {
        lock (_dataWriterLock)
        {
            var wasEnqueuedForObservation = _observationState != State.None;
            _observationState |= state;
            return (_unconsumedBytes == 0 || _waitingForWindowUpdates) && !wasEnqueuedForObservation;
        }
    }

    // Removes consumed bytes from the queue.
    // Returns a bool that represents whether we should schedule this producer to write
    // the remaining bytes.
    internal (bool, bool) Dequeue(long bytes, State state)
    {
        lock (_dataWriterLock)
        {
            _observationState &= ~state;
            _unconsumedBytes -= bytes;
            return (_unconsumedBytes > 0, _observationState != State.None);
        }
    }

    // Consumes bytes from the stream's window and returns the remaining bytes and actual bytes consumed
    internal (long, long) ConsumeWindow(long bytes)
    {
        lock (_dataWriterLock)
        {
            var actual = Math.Min(bytes, _window);
            var remaining = _window -= actual;
            return (actual, remaining);
        }
    }

    // Adds more bytes to the stream's window
    // Returns a bool that represents whether we should schedule this producer to write
    // the remaining bytes.
    private bool UpdateWindow(long bytes)
    {
        lock (_dataWriterLock)
        {
            var wasDepleted = _window <= 0;
            _window += bytes;
            return wasDepleted && _window > 0 && _unconsumedBytes > 0;
        }
    }

    public void StreamReset(uint initialWindowSize)
    {
        // Response should have been completed.
        Debug.Assert(_responseCompleteTaskSource.GetStatus(_responseCompleteTaskSource.Version) == ValueTaskSourceStatus.Succeeded);

        _streamEnded = false;
        _suffixSent = false;
        _startedWritingDataFrames = false;
        _streamCompleted = false;
        _writerComplete = false;
        _pipe.Reset();
        _pipeWriter.Reset();
        _responseCompleteTaskSource.Reset();

        _window = initialWindowSize;
        _unconsumedBytes = 0;
        _observationState = State.None;
        _waitingForWindowUpdates = false;
        WriteHeaders = false;
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

            if (!_streamCompleted)
            {
                var enqueue = Enqueue(_pipeWriter.UnflushedBytes) || EnqueueForObservation(State.Completed);

                // Make sure the writing side is completed.
                _pipeWriter.Complete();

                if (enqueue)
                {
                    Schedule();
                }
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

    // This is called when a CancellationToken fires mid-write. In HTTP/1.x, this aborts the entire connection.
    // For HTTP/2 we abort the stream.
    void IHttpOutputAborter.Abort(ConnectionAbortedException abortReason)
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
            if (_streamCompleted)
            {
                return new ValueTask<FlushResult>(new FlushResult(false, true));
            }

            if (_startedWritingDataFrames)
            {
                var enqueue = Enqueue(_pipeWriter.UnflushedBytes);

                // If there's already been response data written to the stream, just wait for that. Any header
                // should be in front of the data frames in the connection pipe. Trailers could change things.
                var task = _flusher.FlushAsync(this, cancellationToken);

                if (enqueue)
                {
                    Schedule();
                }

                return task;
            }
            else
            {
                var enqueue = EnqueueForObservation(State.FlushHeaders);

                if (enqueue)
                {
                    Schedule();
                }

                return default;
            }
        }
    }

    public void MarkWaitingForWindowUpdates(bool waitingForUpdates)
    {
        lock (_dataWriterLock)
        {
            _waitingForWindowUpdates = waitingForUpdates;
        }
    }

    private void Schedule()
    {
        lock (_dataWriterLock)
        {
            // Lock here
            _waitingForWindowUpdates = false;
        }

        _frameWriter.Schedule(this);
    }

    public ValueTask<FlushResult> Write100ContinueAsync()
    {
        lock (_dataWriterLock)
        {
            ThrowIfSuffixSentOrCompleted();

            if (_streamCompleted)
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
            if (_streamCompleted)
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
                _streamEnded = true;
            }

            WriteHeaders = true;
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
            if (_streamCompleted || data.Length == 0)
            {
                return Task.CompletedTask;
            }

            _startedWritingDataFrames = true;

            _pipeWriter.Write(data);

            var enqueue = Enqueue(data.Length);

            var task = _flusher.FlushAsync(this, cancellationToken).GetAsTask();

            if (enqueue)
            {
                Schedule();
            }

            return task;
        }
    }

    public ValueTask<FlushResult> WriteStreamSuffixAsync()
    {
        lock (_dataWriterLock)
        {
            if (_streamCompleted)
            {
                return GetWaiterTask();
            }

            _streamCompleted = true;
            _suffixSent = true;

            // Try to enqueue any unflushed bytes
            var enqueue = Enqueue(_pipeWriter.UnflushedBytes) || EnqueueForObservation(State.Completed);

            _pipeWriter.Complete();

            if (enqueue)
            {
                Schedule();
            }

            return GetWaiterTask();
        }
    }

    public ValueTask<FlushResult> WriteRstStreamAsync(Http2ErrorCode error)
    {
        lock (_dataWriterLock)
        {
            // Always send the reset even if the response body is _completed. The request body may not have completed yet.
            Stop();

            return _frameWriter.WriteRstStreamAsync(StreamId, error);
        }
    }

    public void Advance(int bytes)
    {
        lock (_dataWriterLock)
        {
            ThrowIfSuffixSentOrCompleted();

            if (_streamCompleted)
            {
                return;
            }

            _startedWritingDataFrames = true;

            _pipeWriter.Advance(bytes);
        }
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        lock (_dataWriterLock)
        {
            ThrowIfSuffixSentOrCompleted();

            if (_streamCompleted)
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

            if (_streamCompleted)
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
            if (_streamCompleted)
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
            if (_streamCompleted || data.Length == 0)
            {
                return new ValueTask<FlushResult>(new FlushResult(false, true));
            }

            _startedWritingDataFrames = true;

            _pipeWriter.Write(data);

            var enqueue = Enqueue(data.Length);
            var task = _flusher.FlushAsync(this, cancellationToken);

            if (enqueue)
            {
                Schedule();
            }

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
            if (_streamCompleted)
            {
                return;
            }

            _streamCompleted = true;

            var enqueue = Enqueue(_pipeWriter.UnflushedBytes) || EnqueueForObservation(State.Cancelled);

            _pipeReader.CancelPendingRead();

            if (enqueue)
            {
                // We need to make sure the cancellation is observed by the code
                Schedule();
            }
        }
    }

    public void Reset()
    {
    }

    internal void CompleteResponse(in FlushResult flushResult)
    {
        _responseCompleteTaskSource.SetResult(flushResult);
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
        lock (_dataWriterLock)
        {
            var maxUpdate = Http2PeerSettings.MaxWindowSize - _window;

            if (bytes > maxUpdate)
            {
                return false;
            }
        }

        if (UpdateWindow(bytes))
        {
            Schedule();
        }

        return true;
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

    private static Pipe CreateDataPipe(MemoryPool<byte> pool)
        => new Pipe(new PipeOptions
        (
            pool: pool,
            readerScheduler: PipeScheduler.Inline,
            writerScheduler: PipeScheduler.ThreadPool,
            pauseWriterThreshold: 1,
            resumeWriterThreshold: 1,
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
        Cancelled = 2,
        Completed = 4
    }
}

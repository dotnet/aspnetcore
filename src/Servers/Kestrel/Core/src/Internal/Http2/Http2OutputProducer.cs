// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal class Http2OutputProducer : IHttpOutputProducer, IHttpOutputAborter, IValueTaskSource<FlushResult>, IDisposable
    {
        private int StreamId => _stream.StreamId;
        private readonly Http2FrameWriter _frameWriter;
        private readonly TimingPipeFlusher _flusher;
        private readonly IKestrelTrace _log;

        // This should only be accessed via the FrameWriter. The connection-level output flow control is protected by the
        // FrameWriter's connection-level write lock.
        private readonly StreamOutputFlowControl _flowControl;
        private readonly MemoryPool<byte> _memoryPool;
        private readonly Http2Stream _stream;
        private readonly object _dataWriterLock = new object();
        private readonly Pipe _pipe;
        private readonly ConcurrentPipeWriter _pipeWriter;
        private readonly PipeReader _pipeReader;
        private readonly ManualResetValueTaskSource<object> _resetAwaitable = new ManualResetValueTaskSource<object>();
        private IMemoryOwner<byte> _fakeMemoryOwner;
        private bool _startedWritingDataFrames;
        private bool _streamCompleted;
        private bool _suffixSent;
        private bool _streamEnded;
        private bool _writerComplete;
        private bool _disposed;

        // Internal for testing
        internal ValueTask _dataWriteProcessingTask;

        /// <summary>The core logic for the IValueTaskSource implementation.</summary>
        private ManualResetValueTaskSourceCore<FlushResult> _responseCompleteTaskSource = new ManualResetValueTaskSourceCore<FlushResult> { RunContinuationsAsynchronously = true }; // mutable struct, do not make this readonly

        // This object is itself usable as a backing source for ValueTask.  Since there's only ever one awaiter
        // for this object's state transitions at a time, we allow the object to be awaited directly. All functionality
        // associated with the implementation is just delegated to the ManualResetValueTaskSourceCore.
        private ValueTask<FlushResult> GetWaiterTask() => new ValueTask<FlushResult>(this, _responseCompleteTaskSource.Version);
        ValueTaskSourceStatus IValueTaskSource<FlushResult>.GetStatus(short token) => _responseCompleteTaskSource.GetStatus(token);
        void IValueTaskSource<FlushResult>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _responseCompleteTaskSource.OnCompleted(continuation, state, token, flags);
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
            _pipeReader = _pipe.Reader;

            // No need to pass in timeoutControl here, since no minDataRates are passed to the TimingPipeFlusher.
            // The minimum output data rate is enforced at the connection level by Http2FrameWriter.
            _flusher = new TimingPipeFlusher(_pipeWriter, timeoutControl: null, _log);

            _dataWriteProcessingTask = ProcessDataWrites();
        }

        public void StreamReset()
        {
            // Data background task must still be running.
            Debug.Assert(!_dataWriteProcessingTask.IsCompleted);
            // Response should have been completed.
            Debug.Assert(_responseCompleteTaskSource.GetStatus(_responseCompleteTaskSource.Version) == ValueTaskSourceStatus.Succeeded);

            _streamEnded = false;
            _suffixSent = false;
            _suffixSent = false;
            _startedWritingDataFrames = false;
            _streamCompleted = false;
            _writerComplete = false;

            _pipe.Reset();
            _pipeWriter.Reset();
            _responseCompleteTaskSource.Reset();

            // Trigger the data process task to resume
            _resetAwaitable.SetResult(null);
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

                // Make sure the writing side is completed.
                _pipeWriter.Complete();

                if (_fakeMemoryOwner != null)
                {
                    _fakeMemoryOwner.Dispose();
                    _fakeMemoryOwner = null;
                }
            }
        }

        // This is called when a CancellationToken fires mid-write. In HTTP/1.x, this aborts the entire connection.
        // For HTTP/2 we abort the stream.
        void IHttpOutputAborter.Abort(ConnectionAbortedException abortReason)
        {
            _stream.ResetAndAbort(abortReason, Http2ErrorCode.INTERNAL_ERROR);
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
                    return default;
                }

                if (_startedWritingDataFrames)
                {
                    // If there's already been response data written to the stream, just wait for that. Any header
                    // should be in front of the data frames in the connection pipe. Trailers could change things.
                    return _flusher.FlushAsync(this, cancellationToken);
                }
                else
                {
                    // Flushing the connection pipe ensures headers already in the pipe are flushed even if no data
                    // frames have been written.
                    return _frameWriter.FlushAsync(this, cancellationToken);
                }
            }
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

        public void WriteResponseHeaders(int statusCode, string ReasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, bool appCompleted)
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
                Http2HeadersFrameFlags http2HeadersFrame;

                if (appCompleted && !_startedWritingDataFrames && (_stream.ResponseTrailers == null || _stream.ResponseTrailers.Count == 0))
                {
                    _streamEnded = true;
                    _stream.DecrementActiveClientStreamCount();
                    http2HeadersFrame = Http2HeadersFrameFlags.END_STREAM;
                }
                else
                {
                    http2HeadersFrame = Http2HeadersFrameFlags.NONE;
                }

                _frameWriter.WriteResponseHeaders(StreamId, statusCode, http2HeadersFrame, responseHeaders);
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
                return _flusher.FlushAsync(this, cancellationToken).GetAsTask();
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

                _pipeWriter.Complete();
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
                    return default;
                }

                _startedWritingDataFrames = true;

                _pipeWriter.Write(data);
                return _flusher.FlushAsync(this, cancellationToken);
            }
        }

        public ValueTask<FlushResult> FirstWriteAsync(int statusCode, string reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, ReadOnlySpan<byte> data, CancellationToken cancellationToken)
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

        public ValueTask<FlushResult> FirstWriteChunkedAsync(int statusCode, string reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, ReadOnlySpan<byte> data, CancellationToken cancellationToken)
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

                _pipeReader.CancelPendingRead();

                _frameWriter.AbortPendingStreamDataWrites(_flowControl);
            }
        }

        public void Reset()
        {
        }

        private async ValueTask ProcessDataWrites()
        {
            // ProcessDataWrites runs for the lifetime of the Http2OutputProducer, and is designed to be reused by multiple streams.
            // When Http2OutputProducer is no longer used (e.g. a stream is aborted and will no longer be used, or the connection is closed)
            // it should be disposed so ProcessDataWrites exits. Not disposing won't cause a memory leak in release builds, but in debug
            // builds active tasks are rooted on Task.s_currentActiveTasks. Dispose could be removed in the future when active tasks are
            // tracked by a weak reference. See https://github.com/dotnet/runtime/issues/26565
            do
            {
                FlushResult flushResult = default;
                ReadResult readResult = default;
                try
                {

                    do
                    {
                        readResult = await _pipeReader.ReadAsync();

                        if (readResult.IsCanceled)
                        {
                            // Response body is aborted, break and complete reader.
                            break;
                        }
                        else if (readResult.IsCompleted && _stream.ResponseTrailers?.Count > 0)
                        {
                            // Output is ending and there are trailers to write
                            // Write any remaining content then write trailers
                            if (readResult.Buffer.Length > 0)
                            {
                                // Only flush if required (i.e. content length exceeds flow control availability)
                                // Writing remaining content without flushing allows content and trailers to be sent in the same packet
                                await _frameWriter.WriteDataAsync(StreamId, _flowControl, readResult.Buffer, endStream: false, forceFlush: false);
                            }

                            _stream.ResponseTrailers.SetReadOnly();
                            _stream.DecrementActiveClientStreamCount();
                            flushResult = await _frameWriter.WriteResponseTrailers(StreamId, _stream.ResponseTrailers);
                        }
                        else if (readResult.IsCompleted && _streamEnded)
                        {
                            if (readResult.Buffer.Length != 0)
                            {
                                ThrowUnexpectedState();
                            }

                            // Headers have already been written and there is no other content to write
                            flushResult = await _frameWriter.FlushAsync(outputAborter: null, cancellationToken: default);
                        }
                        else
                        {
                            var endStream = readResult.IsCompleted;
                            if (endStream)
                            {
                                _stream.DecrementActiveClientStreamCount();
                            }
                            flushResult = await _frameWriter.WriteDataAsync(StreamId, _flowControl, readResult.Buffer, endStream, forceFlush: true);
                        }

                        _pipeReader.AdvanceTo(readResult.Buffer.End);
                    } while (!readResult.IsCompleted);
                }
                catch (Exception ex)
                {
                    _log.LogCritical(ex, nameof(Http2OutputProducer) + "." + nameof(ProcessDataWrites) + " observed an unexpected exception.");
                }

                _pipeReader.Complete();

                // Signal via WriteStreamSuffixAsync to the stream that output has finished.
                // Stream state will move to RequestProcessingStatus.ResponseCompleted
                _responseCompleteTaskSource.SetResult(flushResult);

                if (readResult.IsCompleted)
                {
                    // Successfully read all data. Wait here for the stream to be reset.
                    await new ValueTask(_resetAwaitable, _resetAwaitable.Version);
                    _resetAwaitable.Reset();
                }
                else
                {
                    // Stream was aborted.
                    break;
                }
            } while (!_disposed);

            static void ThrowUnexpectedState()
            {
                throw new InvalidOperationException(nameof(Http2OutputProducer) + "." + nameof(ProcessDataWrites) + " observed an unexpected state where the streams output ended with data still remaining in the pipe.");
            }
        }

        private Memory<byte> GetFakeMemory(int sizeHint)
        {
            if (_fakeMemoryOwner == null)
            {
                _fakeMemoryOwner = _memoryPool.Rent(sizeHint);
            }

            return _fakeMemoryOwner.Memory;
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

            _resetAwaitable.SetResult(null);
        }
    }
}

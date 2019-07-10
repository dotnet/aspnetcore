// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal class Http2OutputProducer : IHttpOutputProducer, IHttpOutputAborter
    {
        private readonly int _streamId;
        private readonly Http2FrameWriter _frameWriter;
        private readonly TimingPipeFlusher _flusher;
        private readonly IKestrelTrace _log;

        // This should only be accessed via the FrameWriter. The connection-level output flow control is protected by the
        // FrameWriter's connection-level write lock.
        private readonly StreamOutputFlowControl _flowControl;
        private readonly MemoryPool<byte> _memoryPool;
        private readonly Http2Stream _stream;
        private readonly object _dataWriterLock = new object();
        private readonly Pipe _dataPipe;
        private readonly ValueTask<FlushResult> _dataWriteProcessingTask;
        private bool _startedWritingDataFrames;
        private bool _completed;
        private bool _suffixSent;
        private bool _streamEnded;
        private bool _disposed;

        private IMemoryOwner<byte> _fakeMemoryOwner;

        public Http2OutputProducer(
            int streamId,
            Http2FrameWriter frameWriter,
            StreamOutputFlowControl flowControl,
            ITimeoutControl timeoutControl,
            MemoryPool<byte> pool,
            Http2Stream stream,
            IKestrelTrace log)
        {
            _streamId = streamId;
            _frameWriter = frameWriter;
            _flowControl = flowControl;
            _memoryPool = pool;
            _stream = stream;
            _log = log;

            _dataPipe = CreateDataPipe(pool);
            _flusher = new TimingPipeFlusher(_dataPipe.Writer, timeoutControl, log);
            _dataWriteProcessingTask = ProcessDataWrites();
        }

        public void Dispose()
        {
            lock (_dataWriterLock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                Stop();

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

        public Task WriteChunkAsync(ReadOnlySpan<byte> span, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<FlushResult>(Task.FromCanceled<FlushResult>(cancellationToken));
            }

            lock (_dataWriterLock)
            {
                ThrowIfSuffixSent();

                if (_completed)
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
                ThrowIfSuffixSent();

                if (_completed)
                {
                    return default;
                }

                return _frameWriter.Write100ContinueAsync(_streamId);
            }
        }

        public void WriteResponseHeaders(int statusCode, string ReasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, bool appCompleted)
        {
            lock (_dataWriterLock)
            {
                // The HPACK header compressor is stateful, if we compress headers for an aborted stream we must send them.
                // Optimize for not compressing or sending them.
                if (_completed)
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
                    http2HeadersFrame = Http2HeadersFrameFlags.END_STREAM;
                }
                else
                {
                    http2HeadersFrame = Http2HeadersFrameFlags.NONE;
                }

                _frameWriter.WriteResponseHeaders(_streamId, statusCode, http2HeadersFrame, responseHeaders);
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
                ThrowIfSuffixSent();

                // This length check is important because we don't want to set _startedWritingDataFrames unless a data
                // frame will actually be written causing the headers to be flushed.
                if (_completed || data.Length == 0)
                {
                    return Task.CompletedTask;
                }

                _startedWritingDataFrames = true;

                _dataPipe.Writer.Write(data);
                return _flusher.FlushAsync(this, cancellationToken).GetAsTask();
            }
        }

        public ValueTask<FlushResult> WriteStreamSuffixAsync()
        {
            lock (_dataWriterLock)
            {
                if (_completed)
                {
                    return _dataWriteProcessingTask;
                }

                _completed = true;
                _suffixSent = true;

                _dataPipe.Writer.Complete();
                return _dataWriteProcessingTask;
            }
        }

        public ValueTask<FlushResult> WriteRstStreamAsync(Http2ErrorCode error)
        {
            lock (_dataWriterLock)
            {
                // Always send the reset even if the response body is _completed. The request body may not have completed yet.
                Stop();

                return _frameWriter.WriteRstStreamAsync(_streamId, error);
            }
        }

        public void Advance(int bytes)
        {
            lock (_dataWriterLock)
            {
                ThrowIfSuffixSent();

                if (_completed)
                {
                    return;
                }

                _startedWritingDataFrames = true;

                _dataPipe.Writer.Advance(bytes);
            }
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            lock (_dataWriterLock)
            {
                ThrowIfSuffixSent();

                if (_completed)
                {
                    return GetFakeMemory(sizeHint).Span;
                }

                return _dataPipe.Writer.GetSpan(sizeHint);
            }
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            lock (_dataWriterLock)
            {
                ThrowIfSuffixSent();

                if (_completed)
                {
                    return GetFakeMemory(sizeHint);
                }

                return _dataPipe.Writer.GetMemory(sizeHint);
            }
        }

        public void CancelPendingFlush()
        {
            lock (_dataWriterLock)
            {
                if (_completed)
                {
                    return;
                }

                _dataPipe.Writer.CancelPendingFlush();
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
                ThrowIfSuffixSent();

                // This length check is important because we don't want to set _startedWritingDataFrames unless a data
                // frame will actually be written causing the headers to be flushed.
                if (_completed || data.Length == 0)
                {
                    return default;
                }

                _startedWritingDataFrames = true;

                _dataPipe.Writer.Write(data);
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
                if (_completed)
                {
                    return;
                }

                _completed = true;

                // Complete with an exception to prevent an end of stream data frame from being sent without an
                // explicit call to WriteStreamSuffixAsync. ConnectionAbortedExceptions are swallowed, so the
                // message doesn't matter
                _dataPipe.Writer.Complete(new OperationCanceledException());

                _frameWriter.AbortPendingStreamDataWrites(_flowControl);
            }
        }

        public void Reset()
        {
        }

        private async ValueTask<FlushResult> ProcessDataWrites()
        {
            FlushResult flushResult = default;
            try
            {
                ReadResult readResult;

                do
                {
                    readResult = await _dataPipe.Reader.ReadAsync();

                    if (readResult.IsCompleted && _stream.ResponseTrailers?.Count > 0)
                    {
                        // Output is ending and there are trailers to write
                        // Write any remaining content then write trailers
                        if (readResult.Buffer.Length > 0)
                        {
                            flushResult = await _frameWriter.WriteDataAsync(_streamId, _flowControl, readResult.Buffer, endStream: false);
                        }

                        _stream.ResponseTrailers.SetReadOnly();
                        flushResult = await _frameWriter.WriteResponseTrailers(_streamId, _stream.ResponseTrailers);
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
                        flushResult = await _frameWriter.WriteDataAsync(_streamId, _flowControl, readResult.Buffer, endStream: readResult.IsCompleted);
                    }

                    _dataPipe.Reader.AdvanceTo(readResult.Buffer.End);
                } while (!readResult.IsCompleted);
            }
            catch (OperationCanceledException)
            {
                // Writes should not throw for aborted streams/connections.
            }
            catch (Exception ex)
            {
                _log.LogCritical(ex, nameof(Http2OutputProducer) + "." + nameof(ProcessDataWrites) + " observed an unexpected exception.");
            }

            _dataPipe.Reader.Complete();

            return flushResult;

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
        private void ThrowIfSuffixSent()
        {
            if (_suffixSent)
            {
                throw new InvalidOperationException("Writing is not allowed after writer was completed.");
            }
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
    }
}

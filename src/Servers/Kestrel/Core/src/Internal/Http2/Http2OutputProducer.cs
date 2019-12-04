// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public class Http2OutputProducer : IHttpOutputProducer, IHttpOutputAborter
    {
        private readonly int _streamId;
        private readonly Http2FrameWriter _frameWriter;
        private readonly TimingPipeFlusher _flusher;

        // This should only be accessed via the FrameWriter. The connection-level output flow control is protected by the
        // FrameWriter's connection-level write lock.
        private readonly StreamOutputFlowControl _flowControl;
        private readonly Http2Stream _stream;
        private readonly object _dataWriterLock = new object();
        private readonly Pipe _dataPipe;
        private readonly Task _dataWriteProcessingTask;
        private bool _startedWritingDataFrames;
        private bool _completed;
        private bool _disposed;

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
            _stream = stream;
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

                if (!_completed)
                {
                    _completed = true;

                    // Complete with an exception to prevent an end of stream data frame from being sent without an
                    // explicit call to WriteStreamSuffixAsync. ConnectionAbortedExceptions are swallowed, so the
                    // message doesn't matter
                    _dataPipe.Writer.Complete(new OperationCanceledException());
                }

                _frameWriter.AbortPendingStreamDataWrites(_flowControl);
            }
        }

        // This is called when a CancellationToken fires mid-write. In HTTP/1.x, this aborts the entire connection.
        // For HTTP/2 we abort the stream.
        void IHttpOutputAborter.Abort(ConnectionAbortedException abortReason)
        {
            _stream.ResetAndAbort(abortReason, Http2ErrorCode.INTERNAL_ERROR);
            Dispose();
        }

        public Task WriteAsync<T>(Func<PipeWriter, T, long> callback, T state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task FlushAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            lock (_dataWriterLock)
            {
                if (_completed)
                {
                    return Task.CompletedTask;
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

        public Task Write100ContinueAsync()
        {
            lock (_dataWriterLock)
            {
                if (_completed)
                {
                    return Task.CompletedTask;
                }

                return _frameWriter.Write100ContinueAsync(_streamId);
            }
        }

        public void WriteResponseHeaders(int statusCode, string ReasonPhrase, HttpResponseHeaders responseHeaders)
        {
            lock (_dataWriterLock)
            {
                // The HPACK header compressor is stateful, if we compress headers for an aborted stream we must send them.
                // Optimize for not compressing or sending them.
                if (_completed)
                {
                    return;
                }

                _frameWriter.WriteResponseHeaders(_streamId, statusCode, responseHeaders);
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
                // This length check is important because we don't want to set _startedWritingDataFrames unless a data
                // frame will actually be written causing the headers to be flushed.
                if (_completed || data.Length == 0)
                {
                    return Task.CompletedTask;
                }

                _startedWritingDataFrames = true;

                _dataPipe.Writer.Write(data);
                return _flusher.FlushAsync(this, cancellationToken);
            }
        }

        public Task WriteStreamSuffixAsync()
        {
            lock (_dataWriterLock)
            {
                if (_completed)
                {
                    return Task.CompletedTask;
                }

                _completed = true;

                _dataPipe.Writer.Complete();
                return _dataWriteProcessingTask;
            }
        }

        public Task WriteRstStreamAsync(Http2ErrorCode error)
        {
            lock (_dataWriterLock)
            {
                // Always send the reset even if the response body is _completed. The request body may not have completed yet.

                Dispose();

                return _frameWriter.WriteRstStreamAsync(_streamId, error);
            }
        }

        private async Task ProcessDataWrites()
        {
            try
            {
                ReadResult readResult;

                do
                {
                    readResult = await _dataPipe.Reader.ReadAsync();

                    if (readResult.IsCompleted && _stream.Trailers?.Count > 0)
                    {
                        if (readResult.Buffer.Length > 0)
                        {
                            await _frameWriter.WriteDataAsync(_streamId, _flowControl, readResult.Buffer, endStream: false);
                        }

                        await _frameWriter.WriteResponseTrailers(_streamId, _stream.Trailers);
                    }
                    else
                    {
                        await _frameWriter.WriteDataAsync(_streamId, _flowControl, readResult.Buffer, endStream: readResult.IsCompleted);
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
                Debug.Assert(false, ex.ToString());
            }

            _dataPipe.Reader.Complete();
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
                minimumSegmentSize: KestrelMemoryPool.MinimumSegmentSize
            ));
    }
}

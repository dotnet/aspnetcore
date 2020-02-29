// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.AspNetCore.Internal;
using System.Net.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal class Http3OutputProducer : IHttpOutputProducer, IHttpOutputAborter
    {
        private readonly Http3FrameWriter _frameWriter;
        private readonly TimingPipeFlusher _flusher;
        private readonly IKestrelTrace _log;
        private readonly MemoryPool<byte> _memoryPool;
        private readonly Http3Stream _stream;
        private readonly PipeWriter _pipeWriter;
        private readonly PipeReader _pipeReader;
        private readonly object _dataWriterLock = new object();
        private readonly ValueTask<FlushResult> _dataWriteProcessingTask;
        private bool _startedWritingDataFrames;
        private bool _completed;
        private bool _disposed;
        private bool _suffixSent;
        private IMemoryOwner<byte> _fakeMemoryOwner;

        public Http3OutputProducer(
             Http3FrameWriter frameWriter,
             MemoryPool<byte> pool,
             Http3Stream stream,
             IKestrelTrace log)
        {
            _frameWriter = frameWriter;
            _memoryPool = pool;
            _stream = stream;
            _log = log;

            var pipe = CreateDataPipe(pool);

            _pipeWriter = pipe.Writer;
            _pipeReader = pipe.Reader;

            _flusher = new TimingPipeFlusher(_pipeWriter, timeoutControl: null, log);
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

        void IHttpOutputAborter.Abort(ConnectionAbortedException abortReason)
        {
            _stream.Abort(abortReason, Http3ErrorCode.InternalError);
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

                _pipeWriter.Advance(bytes);
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

                _pipeWriter.CancelPendingFlush();
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

        public ValueTask<FlushResult> FirstWriteChunkedAsync(int statusCode, string reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, ReadOnlySpan<byte> data, CancellationToken cancellationToken)
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

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            lock (_dataWriterLock)
            {
                ThrowIfSuffixSent();

                if (_completed)
                {
                    return GetFakeMemory(sizeHint);
                }

                return _pipeWriter.GetMemory(sizeHint);
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

                return _pipeWriter.GetSpan(sizeHint);
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
                ThrowSuffixSent();
            }
        }

        [StackTraceHidden]
        private static void ThrowSuffixSent()
        {
            throw new InvalidOperationException("Writing is not allowed after writer was completed.");
        }

        public void Reset()
        {
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

                _pipeWriter.Complete(new OperationCanceledException());

            }
        }

        public ValueTask<FlushResult> Write100ContinueAsync()
        {
            throw new NotImplementedException();
        }

        public ValueTask<FlushResult> WriteChunkAsync(ReadOnlySpan<byte> data, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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

                _pipeWriter.Write(data);
                return _flusher.FlushAsync(this, cancellationToken).GetAsTask();
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

                _pipeWriter.Write(data);
                return _flusher.FlushAsync(this, cancellationToken);
            }
        }

        public void WriteResponseHeaders(int statusCode, string reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, bool appCompleted)
        {
            lock (_dataWriterLock)
            {
                if (_completed)
                {
                    return;
                }

                if (appCompleted && !_startedWritingDataFrames && (_stream.ResponseTrailers == null || _stream.ResponseTrailers.Count == 0))
                {
                    // TODO figure out something to do here.
                }

                _frameWriter.WriteResponseHeaders(statusCode, responseHeaders);
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

                _pipeWriter.Complete();
                return _dataWriteProcessingTask;
            }
        }

        private async ValueTask<FlushResult> ProcessDataWrites()
        {
            FlushResult flushResult = default;
            try
            {
                ReadResult readResult;

                do
                {
                    readResult = await _pipeReader.ReadAsync();

                    if (readResult.IsCompleted && _stream.ResponseTrailers?.Count > 0)
                    {
                        // Output is ending and there are trailers to write
                        // Write any remaining content then write trailers
                        if (readResult.Buffer.Length > 0)
                        {
                            flushResult = await _frameWriter.WriteDataAsync(readResult.Buffer);
                        }

                        _stream.ResponseTrailers.SetReadOnly();
                        flushResult = await _frameWriter.WriteResponseTrailers(_stream.ResponseTrailers);
                    }
                    else if (readResult.IsCompleted)
                    {
                        if (readResult.Buffer.Length != 0)
                        {
                            ThrowUnexpectedState();
                        }

                        // Headers have already been written and there is no other content to write
                        // TODO complete something here.
                        flushResult = await _frameWriter.FlushAsync(outputAborter: null, cancellationToken: default);
                        _frameWriter.Complete();
                    }
                    else
                    {
                        flushResult = await _frameWriter.WriteDataAsync(readResult.Buffer);
                    }

                    _pipeReader.AdvanceTo(readResult.Buffer.End);
                } while (!readResult.IsCompleted);
            }
            catch (OperationCanceledException)
            {
                // Writes should not throw for aborted streams/connections.
            }
            catch (Exception ex)
            {
                _log.LogCritical(ex, nameof(Http3OutputProducer) + "." + nameof(ProcessDataWrites) + " observed an unexpected exception.");
            }

            _pipeReader.Complete();

            return flushResult;

            static void ThrowUnexpectedState()
            {
                throw new InvalidOperationException(nameof(Http3OutputProducer) + "." + nameof(ProcessDataWrites) + " observed an unexpected state where the streams output ended with data still remaining in the pipe.");
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

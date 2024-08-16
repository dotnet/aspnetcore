// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal sealed class Http3OutputProducer : IHttpOutputProducer, IHttpOutputAborter
{
    private readonly Http3FrameWriter _frameWriter;
    private readonly TimingPipeFlusher _flusher;
    private readonly KestrelTrace _log;
    private readonly MemoryPool<byte> _memoryPool;
    private readonly Http3Stream _stream;
    private readonly Pipe _pipe;
    private readonly PipeWriter _pipeWriter;
    private readonly PipeReader _pipeReader;
    private readonly object _dataWriterLock = new object();
    private ValueTask<FlushResult> _dataWriteProcessingTask;
    private bool _startedWritingDataFrames;
    private bool _streamCompleted;
    private bool _disposed;
    private bool _suffixSent;
    private IMemoryOwner<byte>? _fakeMemoryOwner;
    private byte[]? _fakeMemory;

    public Http3OutputProducer(
         Http3FrameWriter frameWriter,
         MemoryPool<byte> pool,
         Http3Stream stream,
         KestrelTrace log)
    {
        _frameWriter = frameWriter;
        _memoryPool = pool;
        _stream = stream;
        _log = log;

        _pipe = CreateDataPipe(pool);

        _pipeWriter = _pipe.Writer;
        _pipeReader = _pipe.Reader;

        _flusher = new TimingPipeFlusher(timeoutControl: null, log);
        _flusher.Initialize(_pipeWriter);
        _dataWriteProcessingTask = ProcessDataWrites().Preserve();
    }

    public void StreamReset()
    {
        // Data background task has finished.
        Debug.Assert(_dataWriteProcessingTask.IsCompleted);

        _suffixSent = false;
        _startedWritingDataFrames = false;
        _streamCompleted = false;

        _pipe.Reset();

        _dataWriteProcessingTask = ProcessDataWrites().Preserve();
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

            if (_fakeMemory != null)
            {
                ArrayPool<byte>.Shared.Return(_fakeMemory);
                _fakeMemory = null;
            }
        }
    }

    // In HTTP/1.x, this aborts the entire connection. For HTTP/3 we abort the stream.
    void IHttpOutputAborter.Abort(ConnectionAbortedException abortReason, ConnectionEndReason reason)
    {
        _stream.Abort(abortReason, Http3ErrorCode.InternalError);
    }

    void IHttpOutputAborter.OnInputOrOutputCompleted()
    {
        _stream.Abort(new ConnectionAbortedException($"{nameof(Http3OutputProducer)}.{nameof(ProcessDataWrites)} has completed."), Http3ErrorCode.InternalError);
    }

    public void Advance(int bytes)
    {
        lock (_dataWriterLock)
        {
            ThrowIfSuffixSent();

            if (_streamCompleted)
            {
                return;
            }

            _startedWritingDataFrames = true;

            _pipeWriter.Advance(bytes);
        }
    }

    public long UnflushedBytes => _pipeWriter.UnflushedBytes;

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

    public ValueTask<FlushResult> FirstWriteAsync(int statusCode, string? reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, ReadOnlySpan<byte> data, CancellationToken cancellationToken)
    {
        lock (_dataWriterLock)
        {
            WriteResponseHeaders(statusCode, reasonPhrase, responseHeaders, autoChunk, appCompleted: false);

            return WriteDataToPipeAsync(data, cancellationToken);
        }
    }

    public ValueTask<FlushResult> FirstWriteChunkedAsync(int statusCode, string? reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, ReadOnlySpan<byte> data, CancellationToken cancellationToken)
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

            if (_streamCompleted)
            {
                return new ValueTask<FlushResult>(new FlushResult(false, true));
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

            if (_streamCompleted)
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

            if (_streamCompleted)
            {
                return GetFakeMemory(sizeHint).Span;
            }

            return _pipeWriter.GetSpan(sizeHint);
        }
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
            if (_streamCompleted)
            {
                return;
            }

            _streamCompleted = true;

            _pipeWriter.Complete(new OperationCanceledException());
        }
    }

    public ValueTask<FlushResult> Write100ContinueAsync()
    {
        lock (_dataWriterLock)
        {
            ThrowIfSuffixSent();

            if (_streamCompleted)
            {
                return default;
            }

            return _frameWriter.Write100ContinueAsync();
        }
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
            if (_streamCompleted || data.Length == 0)
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
            if (_streamCompleted || data.Length == 0)
            {
                return new ValueTask<FlushResult>(new FlushResult(false, true));
            }

            _startedWritingDataFrames = true;

            _pipeWriter.Write(data);
            return _flusher.FlushAsync(this, cancellationToken);
        }
    }

    public void WriteResponseHeaders(int statusCode, string? reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, bool appCompleted)
    {
        // appCompleted flag is not used here. The write FIN is sent via the transport and not via the frame.
        // Headers are written to buffer and flushed with a FIN when Http3FrameWriter.CompleteAsync is called
        // in ProcessDataWrites.

        lock (_dataWriterLock)
        {
            if (_streamCompleted)
            {
                return;
            }

            _frameWriter.WriteResponseHeaders(statusCode, responseHeaders);
        }
    }

    public ValueTask<FlushResult> WriteStreamSuffixAsync()
    {
        lock (_dataWriterLock)
        {
            if (_streamCompleted)
            {
                return _dataWriteProcessingTask;
            }

            _streamCompleted = true;
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
                    flushResult = await _frameWriter.WriteResponseTrailersAsync(_stream.StreamId, _stream.ResponseTrailers);
                }
                else if (readResult.IsCompleted)
                {
                    if (readResult.Buffer.Length != 0)
                    {
                        ThrowUnexpectedState();
                    }

                    // Headers have already been written and there is no other content to write

                    // Need to complete framewriter immediately as CompleteAsync could be called
                    // in the app delegate and we don't want to wait for the app delegate to
                    // finish before sending response.
                    await _frameWriter.CompleteAsync();
                    flushResult = default;
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

        await _pipeReader.CompleteAsync();

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

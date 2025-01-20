// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

#pragma warning disable CA1852 // Seal internal types
internal class Http1OutputProducer : IHttpOutputProducer, IDisposable
#pragma warning restore CA1852 // Seal internal types
{
    private static ReadOnlySpan<byte> ContinueBytes => "HTTP/1.1 100 Continue\r\n\r\n"u8;
    private static ReadOnlySpan<byte> HttpVersion11Bytes => "HTTP/1.1 "u8;
    private static ReadOnlySpan<byte> EndHeadersBytes => "\r\n\r\n"u8;
    private static ReadOnlySpan<byte> EndChunkedResponseBytes => "0\r\n\r\n"u8;

    private const int MaxBeginChunkLength = 10;
    private const int EndChunkLength = 2;

    private readonly string _connectionId;
    private readonly BaseConnectionContext _connectionContext;
    private readonly MemoryPool<byte> _memoryPool;
    private readonly KestrelTrace _log;
    private readonly IHttpMinResponseDataRateFeature _minResponseDataRateFeature;
    private readonly ConnectionMetricsContext _connectionMetricsContext;
    private readonly IHttpOutputAborter _outputAborter;
    private readonly TimingPipeFlusher _flusher;

    // This locks access to all of the below fields
    private readonly Lock _contextLock = new();

    private bool _pipeWriterCompleted;
    private bool _aborted;
    private long _unflushedBytes;
    private int _currentMemoryPrefixBytes;

    private readonly ConcurrentPipeWriter _pipeWriter;
    private IMemoryOwner<byte>? _fakeMemoryOwner;
    private byte[]? _fakeMemory;

    // Chunked responses need to be treated uniquely when using GetMemory + Advance.
    // We need to know the size of the data written to the chunk before calling Advance on the
    // PipeWriter, meaning we internally track how far we have advanced through a current chunk (_advancedBytesForChunk).
    // Once write or flush is called, we modify the _currentChunkMemory to prepend the size of data written
    // and append the end terminator.

    private bool _autoChunk;

    private bool _writeStreamSuffixCalled;

    private int _advancedBytesForChunk;
    private Memory<byte> _currentChunkMemory;
    private bool _currentChunkMemoryUpdated;

    // Fields needed to store writes before calling either startAsync or Write/FlushAsync
    // These should be cleared by the end of the request
    private List<CompletedBuffer>? _completedSegments;
    private int _completedSegmentsByteCount;
    private Memory<byte> _currentSegment;
    private IMemoryOwner<byte>? _currentSegmentOwner;
    private int _position;
    private bool _startCalled;

    public Http1OutputProducer(
        PipeWriter pipeWriter,
        string connectionId,
        BaseConnectionContext connectionContext,
        MemoryPool<byte> memoryPool,
        KestrelTrace log,
        ITimeoutControl timeoutControl,
        IHttpMinResponseDataRateFeature minResponseDataRateFeature,
        ConnectionMetricsContext connectionMetricsContext,
        IHttpOutputAborter outputAborter)
    {
        // Allow appending more data to the PipeWriter when a flush is pending.
        _pipeWriter = new ConcurrentPipeWriter(pipeWriter, memoryPool, _contextLock);
        _connectionId = connectionId;
        _connectionContext = connectionContext;
        _memoryPool = memoryPool;
        _log = log;
        _minResponseDataRateFeature = minResponseDataRateFeature;
        _connectionMetricsContext = connectionMetricsContext;
        _outputAborter = outputAborter;

        _flusher = new TimingPipeFlusher(timeoutControl, log);
        _flusher.Initialize(_pipeWriter);
    }

    public Task WriteDataAsync(ReadOnlySpan<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        return WriteAsync(buffer, cancellationToken).GetAsTask();
    }

    public ValueTask<FlushResult> WriteDataToPipeAsync(ReadOnlySpan<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new ValueTask<FlushResult>(Task.FromCanceled<FlushResult>(cancellationToken));
        }

        return WriteAsync(buffer, cancellationToken);
    }

    public ValueTask<FlushResult> WriteStreamSuffixAsync()
    {
        ValueTask<FlushResult> result = default;

        lock (_contextLock)
        {
            if (!_writeStreamSuffixCalled)
            {
                if (_autoChunk)
                {
                    var writer = new BufferWriter<PipeWriter>(_pipeWriter);
                    result = WriteAsyncInternal(ref writer, EndChunkedResponseBytes);
                }
                else if (_unflushedBytes > 0)
                {
                    result = FlushAsync();
                }

                _writeStreamSuffixCalled = true;
            }
        }

        return result;
    }

    public ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
    {
        lock (_contextLock)
        {
            if (_pipeWriterCompleted)
            {
                return new ValueTask<FlushResult>(new FlushResult(false, true));
            }

            if (_autoChunk)
            {
                if (_advancedBytesForChunk > 0)
                {
                    // If there is data that was chunked before flushing (ex someone did GetMemory->Advance->FlushAsync)
                    // make sure to write whatever was advanced first
                    return FlushAsyncChunked(this, cancellationToken);
                }
                else
                {
                    // If there is an empty write, we still need to update the current chunk
                    _currentChunkMemoryUpdated = false;
                }
            }

            var bytesWritten = _unflushedBytes;
            _unflushedBytes = 0;

            return _flusher.FlushAsync(_minResponseDataRateFeature.MinDataRate, bytesWritten, _outputAborter, cancellationToken);
        }

        static ValueTask<FlushResult> FlushAsyncChunked(Http1OutputProducer producer, CancellationToken token)
        {
            // Local function so in the common-path the stack space for BufferWriter isn't reserved and cleared when it isn't used.

            Debug.Assert(!producer._pipeWriterCompleted);
            Debug.Assert(producer._autoChunk && producer._advancedBytesForChunk > 0);

            var writer = new BufferWriter<PipeWriter>(producer._pipeWriter);
            producer.WriteCurrentChunkMemoryToPipeWriter(ref writer);
            writer.Commit();

            var bytesWritten = producer._unflushedBytes + writer.BytesCommitted;
            producer._unflushedBytes = 0;

            // If there is an empty write, we still need to update the current chunk
            producer._currentChunkMemoryUpdated = false;

            return producer._flusher.FlushAsync(producer._minResponseDataRateFeature.MinDataRate, bytesWritten, producer._outputAborter, token);
        }
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        lock (_contextLock)
        {
            ThrowIfSuffixSent();

            if (_pipeWriterCompleted)
            {
                return GetFakeMemory(sizeHint);
            }
            else if (!_startCalled)
            {
                return LeasedMemory(sizeHint);
            }
            else if (_autoChunk)
            {
                return GetChunkedMemory(sizeHint);
            }
            else
            {
                return _pipeWriter.GetMemory(sizeHint);
            }
        }
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        lock (_contextLock)
        {
            ThrowIfSuffixSent();

            if (_pipeWriterCompleted)
            {
                return GetFakeMemory(sizeHint).Span;
            }
            else if (!_startCalled)
            {
                return LeasedMemory(sizeHint).Span;
            }
            else if (_autoChunk)
            {
                return GetChunkedMemory(sizeHint).Span;
            }
            else
            {
                return _pipeWriter.GetMemory(sizeHint).Span;
            }
        }
    }

    public void Advance(int bytes)
    {
        lock (_contextLock)
        {
            ThrowIfSuffixSent();

            if (_pipeWriterCompleted)
            {
                return;
            }

            if (!_startCalled)
            {
                if (bytes >= 0)
                {
                    if (_currentSegment.Length - bytes < _position)
                    {
                        throw new ArgumentOutOfRangeException(nameof(bytes), "Can't advance past buffer size.");
                    }

                    _position += bytes;
                }
            }
            else if (_autoChunk)
            {
                if (_advancedBytesForChunk > _currentChunkMemory.Length - _currentMemoryPrefixBytes - EndChunkLength - bytes)
                {
                    throw new ArgumentOutOfRangeException(nameof(bytes), "Can't advance past buffer size.");
                }
                _advancedBytesForChunk += bytes;
            }
            else
            {
                _pipeWriter.Advance(bytes);
            }
        }
    }

    public long UnflushedBytes
    {
        get
        {
            var bytes = _position + _advancedBytesForChunk + _pipeWriter.UnflushedBytes + _completedSegmentsByteCount;
            return bytes;
        }
    }

    public void CancelPendingFlush()
    {
        _pipeWriter.CancelPendingFlush();
    }

    // This method is for chunked http responses that directly call response.WriteAsync
    public ValueTask<FlushResult> WriteChunkAsync(ReadOnlySpan<byte> buffer, CancellationToken cancellationToken)
    {
        lock (_contextLock)
        {
            ThrowIfSuffixSent();

            if (_pipeWriterCompleted)
            {
                return default;
            }

            // Make sure any memory used with GetMemory/Advance is written before the chunk
            // passed in.
            if (_advancedBytesForChunk > 0 || buffer.Length > 0)
            {
                var writer = new BufferWriter<PipeWriter>(_pipeWriter);
                CommitChunkInternal(ref writer, buffer);
                _unflushedBytes += writer.BytesCommitted;
            }
        }

        return FlushAsync(cancellationToken);
    }

    private void CommitChunkInternal(ref BufferWriter<PipeWriter> writer, ReadOnlySpan<byte> buffer)
    {
        if (_advancedBytesForChunk > 0)
        {
            WriteCurrentChunkMemoryToPipeWriter(ref writer);
        }

        if (buffer.Length > 0)
        {
            writer.WriteBeginChunkBytes(buffer.Length);
            writer.Write(buffer);
            writer.WriteEndChunkBytes();
        }

        writer.Commit();
    }

    public void WriteResponseHeaders(int statusCode, string? reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, bool appComplete)
    {
        lock (_contextLock)
        {
            ThrowIfSuffixSent();

            if (_pipeWriterCompleted)
            {
                return;
            }

            var buffer = _pipeWriter;
            var writer = new BufferWriter<PipeWriter>(buffer);
            WriteResponseHeadersInternal(ref writer, statusCode, reasonPhrase, responseHeaders, autoChunk);
        }
    }

    private void WriteResponseHeadersInternal(ref BufferWriter<PipeWriter> writer, int statusCode, string? reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk)
    {
        writer.Write(HttpVersion11Bytes);
        var statusBytes = ReasonPhrases.ToStatusBytes(statusCode, reasonPhrase);
        writer.Write(statusBytes);
        responseHeaders.CopyTo(ref writer);
        writer.Write(EndHeadersBytes);

        writer.Commit();

        _autoChunk = autoChunk;
        WriteDataWrittenBeforeHeaders(ref writer);
        _unflushedBytes += writer.BytesCommitted;

        _startCalled = true;
    }

    private void WriteDataWrittenBeforeHeaders(ref BufferWriter<PipeWriter> writer)
    {
        if (_completedSegments != null)
        {
            foreach (var segment in _completedSegments)
            {
                if (_autoChunk)
                {
                    CommitChunkInternal(ref writer, segment.Span);
                }
                else
                {
                    writer.Write(segment.Span);
                    writer.Commit();
                }
                segment.Return();
            }

            _completedSegmentsByteCount = 0;
            _completedSegments.Clear();
        }

        if (!_currentSegment.IsEmpty)
        {
            var segment = _currentSegment.Slice(0, _position);

            if (_autoChunk)
            {
                CommitChunkInternal(ref writer, segment.Span);
            }
            else
            {
                writer.Write(segment.Span);
                writer.Commit();
            }

            _position = 0;

            DisposeCurrentSegment();
        }
    }

    public void Dispose()
    {
        lock (_contextLock)
        {
            _pipeWriter.Abort();

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

            // Call dispose on any memory that wasn't written.
            if (_completedSegments != null)
            {
                foreach (var segment in _completedSegments)
                {
                    segment.Return();
                }
            }

            DisposeCurrentSegment();

            CompletePipe();
        }
    }

    private void DisposeCurrentSegment()
    {
        _currentSegmentOwner?.Dispose();
        _currentSegmentOwner = null;
        _currentSegment = default;
    }

    private void CompletePipe()
    {
        if (!_pipeWriterCompleted)
        {
            _log.ConnectionDisconnect(_connectionId);
            _pipeWriterCompleted = true;
        }
    }

    public void Abort(ConnectionAbortedException error, ConnectionEndReason reason)
    {
        // Abort can be called after Dispose if there's a flush timeout.
        // It's important to still call _lifetimeFeature.Abort() in this case.
        lock (_contextLock)
        {
            if (_aborted)
            {
                return;
            }

            KestrelMetrics.AddConnectionEndReason(_connectionMetricsContext, reason);

            _aborted = true;
            _connectionContext.Abort(error);

            CompletePipe();
        }
    }

    public void Stop()
    {
        lock (_contextLock)
        {
            CompletePipe();
        }
    }

    public ValueTask<FlushResult> Write100ContinueAsync()
    {
        return WriteAsync(ContinueBytes);
    }

    public ValueTask<FlushResult> FirstWriteAsync(int statusCode, string? reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, ReadOnlySpan<byte> buffer, CancellationToken cancellationToken)
    {
        lock (_contextLock)
        {
            ThrowIfSuffixSent();

            if (_pipeWriterCompleted)
            {
                return new ValueTask<FlushResult>(new FlushResult(false, true));
            }

            // Uses same BufferWriter to write response headers and response
            var writer = new BufferWriter<PipeWriter>(_pipeWriter);

            WriteResponseHeadersInternal(ref writer, statusCode, reasonPhrase, responseHeaders, autoChunk);

            return WriteAsyncInternal(ref writer, buffer, cancellationToken);
        }
    }

    public ValueTask<FlushResult> FirstWriteChunkedAsync(int statusCode, string? reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, ReadOnlySpan<byte> buffer, CancellationToken cancellationToken)
    {
        lock (_contextLock)
        {
            ThrowIfSuffixSent();

            if (_pipeWriterCompleted)
            {
                return new ValueTask<FlushResult>(new FlushResult(false, true));
            }

            // Uses same BufferWriter to write response headers and chunk
            var writer = new BufferWriter<PipeWriter>(_pipeWriter);

            WriteResponseHeadersInternal(ref writer, statusCode, reasonPhrase, responseHeaders, autoChunk);

            CommitChunkInternal(ref writer, buffer);

            _unflushedBytes += writer.BytesCommitted;

            return FlushAsync(cancellationToken);
        }
    }

    public void Reset()
    {
        Debug.Assert(_currentSegmentOwner == null);
        Debug.Assert(_completedSegments == null || _completedSegments.Count == 0);
        // Cleared in sequential address ascending order
        _currentMemoryPrefixBytes = 0;
        _autoChunk = false;
        _writeStreamSuffixCalled = false;
        _currentChunkMemoryUpdated = false;
        _startCalled = false;
    }

    private ValueTask<FlushResult> WriteAsync(
        ReadOnlySpan<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        lock (_contextLock)
        {
            ThrowIfSuffixSent();

            if (_pipeWriterCompleted)
            {
                return new ValueTask<FlushResult>(new FlushResult(false, true));
            }

            var writer = new BufferWriter<PipeWriter>(_pipeWriter);
            return WriteAsyncInternal(ref writer, buffer, cancellationToken);
        }
    }

    private ValueTask<FlushResult> WriteAsyncInternal(
        ref BufferWriter<PipeWriter> writer,
        ReadOnlySpan<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        if (_autoChunk)
        {
            if (_advancedBytesForChunk > 0)
            {
                // If there is data that was chunked before writing (ex someone did GetMemory->Advance->WriteAsync)
                // make sure to write whatever was advanced first
                WriteCurrentChunkMemoryToPipeWriter(ref writer);
            }
            else
            {
                // If there is an empty write, we still need to update the current chunk
                _currentChunkMemoryUpdated = false;
            }
        }

        if (buffer.Length > 0)
        {
            writer.Write(buffer);
        }

        writer.Commit();

        var bytesWritten = _unflushedBytes + writer.BytesCommitted;
        _unflushedBytes = 0;

        return _flusher.FlushAsync(
            _minResponseDataRateFeature.MinDataRate,
            bytesWritten,
            _outputAborter,
            cancellationToken);
    }

    private Memory<byte> GetChunkedMemory(int sizeHint)
    {
        if (!_currentChunkMemoryUpdated)
        {
            // Calculating ChunkWriter.GetBeginChunkByteCount isn't free, so instead, we can add
            // the max length for the prefix and suffix and add it to the sizeHint.
            // This still guarantees that the memory passed in will be larger than the sizeHint.
            sizeHint += MaxBeginChunkLength + EndChunkLength;
            UpdateCurrentChunkMemory(sizeHint);
        }
        // Check if we need to allocate a new memory.
        else if (_advancedBytesForChunk >= _currentChunkMemory.Length - _currentMemoryPrefixBytes - EndChunkLength - sizeHint && _advancedBytesForChunk > 0)
        {
            sizeHint += MaxBeginChunkLength + EndChunkLength;
            var writer = new BufferWriter<PipeWriter>(_pipeWriter);
            WriteCurrentChunkMemoryToPipeWriter(ref writer);
            writer.Commit();
            _unflushedBytes += writer.BytesCommitted;

            UpdateCurrentChunkMemory(sizeHint);
        }

        var actualMemory = _currentChunkMemory.Slice(
            _currentMemoryPrefixBytes + _advancedBytesForChunk,
            _currentChunkMemory.Length - _currentMemoryPrefixBytes - EndChunkLength - _advancedBytesForChunk);

        return actualMemory;
    }

    private void UpdateCurrentChunkMemory(int sizeHint)
    {
        _currentChunkMemory = _pipeWriter.GetMemory(sizeHint);
        _currentMemoryPrefixBytes = ChunkWriter.GetPrefixBytesForChunk(_currentChunkMemory.Length, out var sliceOne);
        if (sliceOne)
        {
            _currentChunkMemory = _currentChunkMemory.Slice(0, _currentChunkMemory.Length - 1);
        }
        _currentChunkMemoryUpdated = true;
    }

    private void WriteCurrentChunkMemoryToPipeWriter(ref BufferWriter<PipeWriter> writer)
    {
        Debug.Assert(_advancedBytesForChunk <= _currentChunkMemory.Length);
        Debug.Assert(_advancedBytesForChunk > 0);

        var bytesWritten = writer.WriteBeginChunkBytes(_advancedBytesForChunk);

        Debug.Assert(bytesWritten <= _currentMemoryPrefixBytes);

        if (bytesWritten < _currentMemoryPrefixBytes)
        {
            // If the current chunk of memory isn't completely utilized, we need to copy the contents forwards.
            // This occurs if someone uses less than 255 bytes of the current Memory segment.
            // Therefore, we need to copy it forwards by either 1 or 2 bytes (depending on number of bytes)
            _currentChunkMemory.Slice(_currentMemoryPrefixBytes, _advancedBytesForChunk).CopyTo(_currentChunkMemory.Slice(bytesWritten));
        }

        writer.Advance(_advancedBytesForChunk);
        writer.WriteEndChunkBytes();

        _advancedBytesForChunk = 0;
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

    private Memory<byte> LeasedMemory(int sizeHint)
    {
        EnsureCapacity(sizeHint);
        return _currentSegment.Slice(_position);
    }

    private void EnsureCapacity(int sizeHint)
    {
        // Only subtracts _position from the current segment length if it's non-null.
        // If _currentSegment is null, it returns 0.
        var remainingSize = _currentSegment.Length - _position;

        // If the sizeHint is 0, any capacity will do
        // Otherwise, the buffer must have enough space for the entire size hint, or we need to add a segment.
        if ((sizeHint == 0 && remainingSize > 0) || (sizeHint > 0 && remainingSize >= sizeHint))
        {
            // We have capacity in the current segment
            return;
        }

        AddSegment(sizeHint);
    }

    private void AddSegment(int sizeHint = 0)
    {
        if (_currentSegment.Length != 0)
        {
            // We're adding a segment to the list
            if (_completedSegments == null)
            {
                _completedSegments = new List<CompletedBuffer>();
            }

            // Position might be less than the segment length if there wasn't enough space to satisfy the sizeHint when
            // GetMemory was called. In that case we'll take the current segment and call it "completed", but need to
            // ignore any empty space in it.
            _completedSegments.Add(new CompletedBuffer(_currentSegmentOwner, _currentSegment, _position));
            _completedSegmentsByteCount += _position;
        }

        if (sizeHint <= _memoryPool.MaxBufferSize)
        {
            // Get a new buffer using the minimum segment size, unless the size hint is larger than a single segment.
            // Also, the size cannot be larger than the MaxBufferSize of the MemoryPool
            var owner = _memoryPool.Rent(sizeHint);
            _currentSegment = owner.Memory;
            _currentSegmentOwner = owner;
        }
        else if (sizeHint <= MemoryPool<byte>.Shared.MaxBufferSize)
        {
            // fallback to ArrayPool instead of the passed in memory pool (default is PinnedBlockMemoryPool)
            // PinnedBlockMemoryPool currently defaults to a low (4k) max buffer size while ArrayPool is 2G
            var owner = MemoryPool<byte>.Shared.Rent(sizeHint);
            _currentSegment = owner.Memory;
            _currentSegmentOwner = owner;
        }
        else
        {
            _currentSegment = new byte[sizeHint];
            _currentSegmentOwner = null;
        }

        _position = 0;
    }

    [StackTraceHidden]
    private void ThrowIfSuffixSent()
    {
        if (_writeStreamSuffixCalled)
        {
            ThrowSuffixSent();
        }
    }

    [StackTraceHidden]
    private static void ThrowSuffixSent()
    {
        throw new InvalidOperationException("Writing is not allowed after writer was completed.");
    }

    /// <summary>
    /// Holds a byte[] from the pool and a size value. Basically a Memory but guaranteed to be backed by an ArrayPool byte[], so that we know we can return it.
    /// </summary>
    private readonly struct CompletedBuffer
    {
        private readonly IMemoryOwner<byte>? _memoryOwner;

        public Memory<byte> Buffer { get; }
        public int Length { get; }

        public ReadOnlySpan<byte> Span => Buffer.Span.Slice(0, Length);

        public CompletedBuffer(IMemoryOwner<byte>? owner, Memory<byte> buffer, int length)
        {
            _memoryOwner = owner;

            Buffer = buffer;
            Length = length;
        }

        public void Return()
        {
            _memoryOwner?.Dispose();
        }
    }
}

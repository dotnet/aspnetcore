// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public class Http1OutputProducer : IHttpOutputProducer, IHttpOutputAborter, IDisposable
    {
        // Use C#7.3's ReadOnlySpan<byte> optimization for static data https://vcsjones.com/2019/02/01/csharp-readonly-span-bytes-static/
        // "HTTP/1.1 100 Continue\r\n\r\n"
        private static ReadOnlySpan<byte> ContinueBytes => new byte[] { (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'1', (byte)'.', (byte)'1', (byte)' ', (byte)'1', (byte)'0', (byte)'0', (byte)' ', (byte)'C', (byte)'o', (byte)'n', (byte)'t', (byte)'i', (byte)'n', (byte)'u', (byte)'e', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };
        // "HTTP/1.1 "
        private static ReadOnlySpan<byte> HttpVersion11Bytes => new byte[] { (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'1', (byte)'.', (byte)'1', (byte)' ' };
        // "\r\n\r\n"
        private static ReadOnlySpan<byte> EndHeadersBytes => new byte[] { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };
        // "0\r\n\r\n"
        private static ReadOnlySpan<byte> EndChunkedResponseBytes => new byte[] { (byte)'0', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };

        private readonly string _connectionId;
        private readonly ConnectionContext _connectionContext;
        private readonly IKestrelTrace _log;
        private readonly IHttpMinResponseDataRateFeature _minResponseDataRateFeature;
        private readonly TimingPipeFlusher _flusher;
        private readonly MemoryPool<byte> _memoryPool;

        // This locks access to all of the below fields
        private readonly object _contextLock = new object();

        private bool _pipeWriterCompleted;
        private bool _completed;
        private bool _aborted;
        private long _unflushedBytes;
        private bool _autoChunk;
        private readonly PipeWriter _pipeWriter;
        private const int MemorySizeThreshold = 1024;
        private const int BeginChunkLengthMax = 5;
        private const int EndChunkLength = 2;

        // Chunked responses need to be treated uniquely when using GetMemory + Advance.
        // We need to know the size of the data written to the chunk before calling Advance on the
        // PipeWriter, meaning we internally track how far we have advanced through a current chunk (_advancedBytesForChunk).
        // Once write or flush is called, we modify the _currentChunkMemory to prepend the size of data written
        // and append the end terminator.
        private int _advancedBytesForChunk;
        private Memory<byte> _currentChunkMemory;
        private bool _currentChunkMemoryUpdated;
        private IMemoryOwner<byte> _fakeMemoryOwner;

        public Http1OutputProducer(
            PipeWriter pipeWriter,
            string connectionId,
            ConnectionContext connectionContext,
            IKestrelTrace log,
            ITimeoutControl timeoutControl,
            IHttpMinResponseDataRateFeature minResponseDataRateFeature,
            MemoryPool<byte> memoryPool)
        {
            _pipeWriter = pipeWriter;
            _connectionId = connectionId;
            _connectionContext = connectionContext;
            _log = log;
            _minResponseDataRateFeature = minResponseDataRateFeature;
            _flusher = new TimingPipeFlusher(pipeWriter, timeoutControl, log);
            _memoryPool = memoryPool;
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
            return WriteAsync(EndChunkedResponseBytes);
        }

        public ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            lock (_contextLock)
            {
                if (_pipeWriterCompleted)
                {
                    return default;
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

                return _flusher.FlushAsync(_minResponseDataRateFeature.MinDataRate, bytesWritten, this, cancellationToken);
            }

            ValueTask<FlushResult> FlushAsyncChunked(Http1OutputProducer producer, CancellationToken token)
            {
                // Local function so in the common-path the stack space for BufferWriter isn't reservered and cleared when it isn't used.

                Debug.Assert(!producer._pipeWriterCompleted);
                Debug.Assert(producer._autoChunk && producer._advancedBytesForChunk > 0);

                var writer = new BufferWriter<PipeWriter>(producer._pipeWriter);
                producer.WriteCurrentMemoryToPipeWriter(ref writer);
                writer.Commit();

                var bytesWritten = producer._unflushedBytes + writer.BytesCommitted;
                producer._unflushedBytes = 0;

                // If there is an empty write, we still need to update the current chunk
                producer._currentChunkMemoryUpdated = false;

                return producer._flusher.FlushAsync(producer._minResponseDataRateFeature.MinDataRate, bytesWritten, producer, token);
            }
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            lock (_contextLock)
            {
                if (_completed)
                {
                    return GetFakeMemory(sizeHint);
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
                if (_completed)
                {
                    return GetFakeMemory(sizeHint).Span;
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
                if (_completed)
                {
                    return;
                }

                if (_autoChunk)
                {
                    if (bytes < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(bytes));
                    }

                    if (bytes + _advancedBytesForChunk > _currentChunkMemory.Length - BeginChunkLengthMax - EndChunkLength)
                    {
                        throw new InvalidOperationException("Can't advance past buffer size.");
                    }
                    _advancedBytesForChunk += bytes;
                }
                else
                {
                    _pipeWriter.Advance(bytes);
                }
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
                }
            }

            return FlushAsync(cancellationToken);
        }

        private void CommitChunkInternal(ref BufferWriter<PipeWriter> writer, ReadOnlySpan<byte> buffer)
        {
            if (_advancedBytesForChunk > 0)
            {
                WriteCurrentMemoryToPipeWriter(ref writer);
            }

            if (buffer.Length > 0)
            {

                writer.WriteBeginChunkBytes(buffer.Length);
                writer.Write(buffer);
                writer.WriteEndChunkBytes();
            }

            writer.Commit();
            _unflushedBytes += writer.BytesCommitted;
        }

        public void WriteResponseHeaders(int statusCode, string reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk)
        {
            lock (_contextLock)
            {
                if (_pipeWriterCompleted)
                {
                    return;
                }

                var buffer = _pipeWriter;
                var writer = new BufferWriter<PipeWriter>(buffer);
                WriteResponseHeadersInternal(ref writer, statusCode, reasonPhrase, responseHeaders, autoChunk);
            }
        }

        private void WriteResponseHeadersInternal(ref BufferWriter<PipeWriter> writer, int statusCode, string reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk)
        {
            writer.Write(HttpVersion11Bytes);
            var statusBytes = ReasonPhrases.ToStatusBytes(statusCode, reasonPhrase);
            writer.Write(statusBytes);
            responseHeaders.CopyTo(ref writer);
            writer.Write(EndHeadersBytes);

            writer.Commit();

            _unflushedBytes += writer.BytesCommitted;
            _autoChunk = autoChunk;
        }

        public void Dispose()
        {
            lock (_contextLock)
            {
                if (_fakeMemoryOwner != null)
                {
                    _fakeMemoryOwner.Dispose();
                    _fakeMemoryOwner = null;
                }

                CompletePipe();
            }
        }

        private void CompletePipe()
        {
            if (!_pipeWriterCompleted)
            {
                _log.ConnectionDisconnect(_connectionId);
                _pipeWriterCompleted = true;
                _completed = true;
                _pipeWriter.Complete();
            }
        }

        public void Abort(ConnectionAbortedException error)
        {
            // Abort can be called after Dispose if there's a flush timeout.
            // It's important to still call _lifetimeFeature.Abort() in this case.
            lock (_contextLock)
            {
                if (_aborted)
                {
                    return;
                }

                _aborted = true;
                _connectionContext.Abort(error);

                CompletePipe();
            }
        }

        public void Complete()
        {
            lock (_contextLock)
            {
                _completed = true;
            }
        }

        public ValueTask<FlushResult> Write100ContinueAsync()
        {
            return WriteAsync(ContinueBytes);
        }

        public ValueTask<FlushResult> FirstWriteAsync(int statusCode, string reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, ReadOnlySpan<byte> buffer, CancellationToken cancellationToken)
        {
            lock (_contextLock)
            {
                if (_pipeWriterCompleted)
                {
                    return default;
                }

                // Uses same BufferWriter to write response headers and response
                var writer = new BufferWriter<PipeWriter>(_pipeWriter);

                WriteResponseHeadersInternal(ref writer, statusCode, reasonPhrase, responseHeaders, autoChunk);

                return WriteAsyncInternal(ref writer, buffer, cancellationToken);
            }
        }

        public ValueTask<FlushResult> FirstWriteChunkedAsync(int statusCode, string reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, ReadOnlySpan<byte> buffer, CancellationToken cancellationToken)
        {
            lock (_contextLock)
            {
                if (_pipeWriterCompleted)
                {
                    return default;
                }

                // Uses same BufferWriter to write response headers and chunk
                var writer = new BufferWriter<PipeWriter>(_pipeWriter);

                WriteResponseHeadersInternal(ref writer, statusCode, reasonPhrase, responseHeaders, autoChunk);

                CommitChunkInternal(ref writer, buffer);

                return FlushAsync(cancellationToken);
            }
        }

        private ValueTask<FlushResult> WriteAsync(
            ReadOnlySpan<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            lock (_contextLock)
            {
                if (_pipeWriterCompleted)
                {
                    return default;
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
                    WriteCurrentMemoryToPipeWriter(ref writer);
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
                this,
                cancellationToken);
        }

        // These methods are for chunked http responses that use GetMemory/Advance
        private Memory<byte> GetChunkedMemory(int sizeHint)
        {
            // The max size of a chunk will be the size of memory returned from the PipeWriter (today 4096)
            // minus 5 for the max chunked prefix size and minus 2 for the chunked ending, leaving a total of
            // 4089.

            if (!_currentChunkMemoryUpdated)
            {
                // First time calling GetMemory
                _currentChunkMemory = _pipeWriter.GetMemory(sizeHint);
                _currentChunkMemoryUpdated = true;
            }

            var memoryMaxLength = _currentChunkMemory.Length - BeginChunkLengthMax - EndChunkLength;
            if (_advancedBytesForChunk >= memoryMaxLength - Math.Min(MemorySizeThreshold, sizeHint))
            {
                // Chunk is completely written, commit it to the pipe so GetMemory will return a new chunk of memory.
                var writer = new BufferWriter<PipeWriter>(_pipeWriter);
                WriteCurrentMemoryToPipeWriter(ref writer);
                writer.Commit();

                _unflushedBytes += writer.BytesCommitted;
                _currentChunkMemory = _pipeWriter.GetMemory(sizeHint);
                _currentChunkMemoryUpdated = true;
            }

            var actualMemory = _currentChunkMemory.Slice(
                BeginChunkLengthMax + _advancedBytesForChunk,
                memoryMaxLength - _advancedBytesForChunk);

            Debug.Assert(actualMemory.Length <= 4089);

            return actualMemory;
        }

        private void WriteCurrentMemoryToPipeWriter(ref BufferWriter<PipeWriter> writer)
        {
            Debug.Assert(_advancedBytesForChunk <= _currentChunkMemory.Length);
            Debug.Assert(_advancedBytesForChunk > 0);

            var bytesWritten = writer.WriteBeginChunkBytes(_advancedBytesForChunk);

            Debug.Assert(bytesWritten <= BeginChunkLengthMax);

            if (bytesWritten < BeginChunkLengthMax)
            {
                // If the current chunk of memory isn't completely utilized, we need to copy the contents forwards.
                // This occurs if someone uses less than 255 bytes of the current Memory segment.
                // Therefore, we need to copy it forwards by either 1 or 2 bytes (depending on number of bytes)
                _currentChunkMemory.Slice(BeginChunkLengthMax, _advancedBytesForChunk).CopyTo(_currentChunkMemory.Slice(bytesWritten));
            }

            writer.Advance(_advancedBytesForChunk);
            writer.WriteEndChunkBytes();

            _advancedBytesForChunk = 0;
        }

        private Memory<byte> GetFakeMemory(int sizeHint)
        {
            if (_fakeMemoryOwner == null)
            {
                _fakeMemoryOwner = _memoryPool.Rent(sizeHint);
            }
            return _fakeMemoryOwner.Memory;
        }
    }
}

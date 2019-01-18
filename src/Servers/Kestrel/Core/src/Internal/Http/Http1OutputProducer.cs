// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public class Http1OutputProducer : IHttpOutputProducer, IHttpOutputAborter, IDisposable
    {
        private static readonly ReadOnlyMemory<byte> _continueBytes = new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n"));
        private static readonly byte[] _bytesHttpVersion11 = Encoding.ASCII.GetBytes("HTTP/1.1 ");
        private static readonly byte[] _bytesEndHeaders = Encoding.ASCII.GetBytes("\r\n\r\n");
        private static readonly ReadOnlyMemory<byte> _endChunkedResponseBytes = new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("0\r\n\r\n"));

        private readonly string _connectionId;
        private readonly ConnectionContext _connectionContext;
        private readonly IKestrelTrace _log;
        private readonly IHttpMinResponseDataRateFeature _minResponseDataRateFeature;
        private readonly TimingPipeFlusher _flusher;

        // This locks access to all of the below fields
        private readonly object _contextLock = new object();

        private bool _completed = false;
        private bool _aborted;
        private long _unflushedBytes;
        private bool _autoChunk;
        private readonly PipeWriter _pipeWriter;

        private const int BeginChunkLengthMax = 5;
        private const int EndChunkLength = 2;

        // Chunked responses need to be treated uniquely when using GetMemory + Advance.
        // We need to know the size of the data written to the chunk before calling Advance on the
        // PipeWriter, meaning we internally track how far we have advanced through a current chunk (_advancedBytesForChunk).
        // Once write or flush is called, we modify the _currentChunkMemory to prepend the size of data written
        // and append the end terminator.
        private int _advancedBytesForChunk;
        private Memory<byte> _currentChunkMemory;

        public Http1OutputProducer(
            PipeWriter pipeWriter,
            string connectionId,
            ConnectionContext connectionContext,
            IKestrelTrace log,
            ITimeoutControl timeoutControl,
            IHttpMinResponseDataRateFeature minResponseDataRateFeature)
        {
            _pipeWriter = pipeWriter;
            _connectionId = connectionId;
            _connectionContext = connectionContext;
            _log = log;
            _minResponseDataRateFeature = minResponseDataRateFeature;
            _flusher = new TimingPipeFlusher(pipeWriter, timeoutControl, log);
        }

        public Task WriteDataAsync(ReadOnlySpan<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            return WriteAsync(buffer, cancellationToken).AsTask();
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
            return WriteAsync(_endChunkedResponseBytes.Span);
        }

        public ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            return WriteAsync(Constants.EmptyData, cancellationToken);
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (_autoChunk)
            {
                return GetChunkedMemory(sizeHint);
            }
            else
            {
                return _pipeWriter.GetMemory(sizeHint);
            }
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            if (_autoChunk)
            {
                return GetChunkedMemory(sizeHint).Span;
            }
            else
            {
                return _pipeWriter.GetMemory(sizeHint).Span;
            }
        }

        public void Advance(int bytes)
        {
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

        public void CancelPendingFlush()
        {
            // TODO we may not want to support this.
            _pipeWriter.CancelPendingFlush();
        }

        // This method is for chunked http responses
        public ValueTask<FlushResult> WriteChunkAsync(ReadOnlySpan<byte> buffer, CancellationToken cancellationToken)
        {
            lock (_contextLock)
            {
                if (_completed)
                {
                    return default;
                }

                CommitChunkToPipe();

                if (buffer.Length > 0)
                {
                    var writer = new BufferWriter<PipeWriter>(_pipeWriter);

                    writer.WriteBeginChunkBytes(buffer.Length);
                    writer.Write(buffer);
                    writer.WriteEndChunkBytes();
                    writer.Commit();

                    _unflushedBytes += writer.BytesCommitted;
                }
            }

            return FlushAsync(cancellationToken);
        }

        public void WriteResponseHeaders(int statusCode, string reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk)
        {
            lock (_contextLock)
            {
                if (_completed)
                {
                    return;
                }

                var buffer = _pipeWriter;
                var writer = new BufferWriter<PipeWriter>(buffer);

                writer.Write(_bytesHttpVersion11);
                var statusBytes = ReasonPhrases.ToStatusBytes(statusCode, reasonPhrase);
                writer.Write(statusBytes);
                responseHeaders.CopyTo(ref writer);
                writer.Write(_bytesEndHeaders);

                writer.Commit();

                _unflushedBytes += writer.BytesCommitted;
                _autoChunk = autoChunk;
            }
        }

        public void Dispose()
        {
            lock (_contextLock)
            {
                if (_completed)
                {
                    return;
                }

                _log.ConnectionDisconnect(_connectionId);
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
                Dispose();
            }
        }

        public ValueTask<FlushResult> Write100ContinueAsync()
        {
            return WriteAsync(_continueBytes.Span);
        }

        public void Complete(Exception exception = null)
        {
            // TODO What to do with exception.
            // and how to handle writing to response here.
        }

        private ValueTask<FlushResult> WriteAsync(
            ReadOnlySpan<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            lock (_contextLock)
            {
                if (_completed)
                {
                    return default;
                }

                if (_autoChunk)
                {
                    // If there is data that was chunked before writing (ex someone did GetMemory->Advance->WriteAsync)
                    // make sure to write whatever was advanced first
                    CommitChunkToPipe();
                }

                var writer = new BufferWriter<PipeWriter>(_pipeWriter);
                if (buffer.Length > 0)
                {
                    writer.Write(buffer);

                    _unflushedBytes += buffer.Length;
                }
                writer.Commit();

                var bytesWritten = _unflushedBytes;
                _unflushedBytes = 0;

                return _flusher.FlushAsync(
                    _minResponseDataRateFeature.MinDataRate,
                    bytesWritten,
                    this,
                    cancellationToken);
            }
        }

        private Memory<byte> GetChunkedMemory(int sizeHint)
        {
            // The max size of a chunk will be the size of memory returned from the PipeWriter (today 4096)
            // minus 5 for the max chunked prefix size and minus 2 for the chunked ending, leaving a total of
            // 4089.

            if (_currentChunkMemory.Length == 0)
            {
                // First time calling GetMemory
                _currentChunkMemory = _pipeWriter.GetMemory(sizeHint);
            }

            var memoryMaxLength = _currentChunkMemory.Length - BeginChunkLengthMax - EndChunkLength;
            if (_advancedBytesForChunk == memoryMaxLength)
            {
                // Chunk is completely written, commit it to the pipe so GetMemory will return a new chunk of memory.
                CommitChunkToPipe();
                _currentChunkMemory = _pipeWriter.GetMemory(sizeHint);
            }

            var actualMemory = _currentChunkMemory.Slice(
                BeginChunkLengthMax + _advancedBytesForChunk,
                memoryMaxLength - _advancedBytesForChunk);

            return actualMemory;
        }

        private void CommitChunkToPipe()
        {
            var writer = new BufferWriter<PipeWriter>(_pipeWriter);

            Debug.Assert(_advancedBytesForChunk <= _currentChunkMemory.Length);

            if (_advancedBytesForChunk > 0)
            {
                var bytesWritten = writer.WriteBeginChunkBytes(_advancedBytesForChunk);
                if (bytesWritten < BeginChunkLengthMax)
                {
                    // If the current chunk of memory isn't completely utilized, we need to copy the contents forwards.
                    // This occurs if someone uses less than 255 bytes of the current Memory segment.
                    // Therefore, we need to copy it forwards by either 1 or 2 bytes (depending on number of bytes)
                    _currentChunkMemory.Slice(BeginChunkLengthMax, _advancedBytesForChunk).CopyTo(_currentChunkMemory.Slice(bytesWritten));
                }

                writer.Write(_currentChunkMemory.Slice(bytesWritten, _advancedBytesForChunk).Span);
                writer.WriteEndChunkBytes();
                writer.Commit();
                _advancedBytesForChunk = 0;
            }
        }
    }
}

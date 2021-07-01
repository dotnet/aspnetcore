// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

#if COMPONENTS_SERVER
namespace Microsoft.AspNetCore.Components.Server.Circuits
#elif BLAZOR_WEBVIEW
namespace Microsoft.AspNetCore.Components.WebView
#elif BLAZOR_WEBASSEMBLY
namespace Microsoft.AspNetCore.Components.WebAssembly.Infrastructure
#endif
{
    internal abstract class BaseJSDataStream : Stream
    {
        private readonly Dictionary<long, BaseJSDataStream> _dataStreamInstances;
        private readonly long _streamId;
        private readonly long _totalLength;
        private readonly TimeSpan _jsInteropDefaultCallTimeout;
        private readonly CancellationToken _streamCancellationToken;
        private readonly Stream _pipeReaderStream;
        private readonly Pipe _pipe;
        private long _bytesRead;
        private long _expectedChunkId;
        private DateTimeOffset _lastDataReceivedTime;
        private bool _disposed;

        protected BaseJSDataStream(
            Dictionary<long, BaseJSDataStream> dataStreamInstances,
            long streamId,
            long totalLength,
            long maxBufferSize,
            TimeSpan jsInteropDefaultCallTimeout,
            CancellationToken cancellationToken)
        {
            _dataStreamInstances = dataStreamInstances;
            _streamId = streamId;
            _totalLength = totalLength;
            _jsInteropDefaultCallTimeout = jsInteropDefaultCallTimeout;
            _streamCancellationToken = cancellationToken;

            _lastDataReceivedTime = DateTimeOffset.UtcNow;
            _ = ThrowOnTimeout();

            _dataStreamInstances.Add(_streamId, this);

            _pipe = new Pipe(new PipeOptions(pauseWriterThreshold: maxBufferSize, resumeWriterThreshold: maxBufferSize / 2));
            _pipeReaderStream = _pipe.Reader.AsStream();
        }

        internal async Task<bool> ReceiveData(long chunkId, byte[] chunk, string error)
        {
            try
            {
                _lastDataReceivedTime = DateTimeOffset.UtcNow;
                _ = ThrowOnTimeout();

                if (!string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException($"An error occurred while reading the remote stream: {error}");
                }

                if (chunkId != _expectedChunkId)
                {
                    throw new EndOfStreamException($"Out of sequence chunk received, expected {_expectedChunkId}, but received {chunkId}.");
                }

                ++_expectedChunkId;

                if (chunk.Length == 0)
                {
                    throw new EndOfStreamException($"The incoming data chunk cannot be empty.");
                }

                _bytesRead += chunk.Length;

                if (_bytesRead > _totalLength)
                {
                    throw new EndOfStreamException($"The incoming data stream declared a length {_totalLength}, but {_bytesRead} bytes were sent.");
                }

                await _pipe.Writer.WriteAsync(chunk, _streamCancellationToken);

                if (_bytesRead == _totalLength)
                {
                    await CompletePipeAndDisposeStream();
                }

                return true;
            }
            catch (Exception e)
            {
                await CompletePipeAndDisposeStream(e);

                // Fatal exception, crush the circuit. A well behaved client
                // should not result in this type of exception.
                if (e is EndOfStreamException)
                {
                    throw;
                }

                return false;
            }
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _totalLength;

        public override long Position
        {
            get => _pipeReaderStream.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
            => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException("Synchronous reads are not supported.");

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var linkedCancellationToken = GetLinkedCancellationToken(_streamCancellationToken, cancellationToken);
            return await _pipeReaderStream.ReadAsync(buffer.AsMemory(offset, count), linkedCancellationToken);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var linkedCancellationToken = GetLinkedCancellationToken(_streamCancellationToken, cancellationToken);
            return await _pipeReaderStream.ReadAsync(buffer, linkedCancellationToken);
        }

        private static CancellationToken GetLinkedCancellationToken(CancellationToken a, CancellationToken b)
        {
            if (a.CanBeCanceled && b.CanBeCanceled)
            {
                return CancellationTokenSource.CreateLinkedTokenSource(a, b).Token;
            }
            else if (a.CanBeCanceled)
            {
                return a;
            }

            return b;
        }

        private async Task ThrowOnTimeout()
        {
            await Task.Delay(_jsInteropDefaultCallTimeout);

            if (!_disposed && (DateTimeOffset.UtcNow >= _lastDataReceivedTime.Add(_jsInteropDefaultCallTimeout)))
            {
                // Dispose of the stream if a chunk isn't received within the jsInteropDefaultCallTimeout.
                var timeoutException = new TimeoutException("Did not receive any data in the alloted time.");
                await CompletePipeAndDisposeStream(timeoutException);
                RaiseUnhandledException(timeoutException);
            }
        }

        protected abstract void RaiseUnhandledException(TimeoutException timeoutException);

        internal async Task CompletePipeAndDisposeStream(Exception? ex = null)
        {
            await _pipe.Writer.CompleteAsync(ex);
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dataStreamInstances.Remove(_streamId);
            }

            _disposed = true;
        }
    }
}

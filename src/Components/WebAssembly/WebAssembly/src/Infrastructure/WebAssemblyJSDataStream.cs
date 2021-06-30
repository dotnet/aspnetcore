// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class WebAssemblyJSDataStream : Stream
    {
        private readonly DefaultWebAssemblyJSRuntime _runtime;
        private readonly long _streamId;
        private readonly long _totalLength;
        private readonly TimeSpan _defaultCallTimeout;
        private readonly CancellationToken _streamCancellationToken;
        private readonly Stream _pipeReaderStream;
        private readonly Pipe _pipe;
        private long _bytesRead;
        private long _expectedChunkId;
        private DateTimeOffset _lastDataReceivedTime;
        private bool _disposed;

        public static async Task<bool> ReceiveData(DefaultWebAssemblyJSRuntime runtime, long streamId, long chunkId, byte[] chunk, string error)
        {
            if (!runtime.WebAssemblyJSDataStreamInstances.TryGetValue(streamId, out var instance))
            {
                // There is no data stream with the given identifier. It may have already been disposed.
                // We notify JS that the stream has been cancelled/disposed.
                return false;
            }

            return await instance.ReceiveData(chunkId, chunk, error);
        }

        internal static async ValueTask<WebAssemblyJSDataStream> CreateWebAssemblyJSDataStreamAsync(
            DefaultWebAssemblyJSRuntime runtime,
            IJSStreamReference jsStreamReference,
            long totalLength,
            long maxBufferSize,
            long chunkSize,
            TimeSpan defaultCallTimeout,
            CancellationToken cancellationToken = default)
        {
            var streamId = runtime.WebAssemblyJSDataStreamNextInstanceId++;
            var webAssemblyJSDataStream = new WebAssemblyJSDataStream(runtime, streamId, totalLength, maxBufferSize, defaultCallTimeout, cancellationToken);
            await runtime.InvokeVoidAsync("Blazor._internal.sendJSDataStream", jsStreamReference, streamId, chunkSize);
            return webAssemblyJSDataStream;
        }

        private WebAssemblyJSDataStream(
            DefaultWebAssemblyJSRuntime runtime,
            long streamId,
            long totalLength,
            long maxBufferSize,
            TimeSpan defaultCallTimeout,
            CancellationToken cancellationToken)
        {
            _runtime = runtime;
            _streamId = streamId;
            _totalLength = totalLength;
            _defaultCallTimeout = defaultCallTimeout;
            _streamCancellationToken = cancellationToken;

            _lastDataReceivedTime = DateTime.UtcNow;
            _ = ThrowOnTimeout();

            _runtime.WebAssemblyJSDataStreamInstances.Add(_streamId, this);

            _pipe = new Pipe(new PipeOptions(pauseWriterThreshold: maxBufferSize, resumeWriterThreshold: maxBufferSize / 2));
            _pipeReaderStream = _pipe.Reader.AsStream();
        }

        private async Task<bool> ReceiveData(long chunkId, byte[] chunk, string error)
        {
            try
            {
                _lastDataReceivedTime = DateTime.UtcNow;
                _ = ThrowOnTimeout();

                if (!string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException($"An error occurred while reading the WebAssembly stream: {error}");
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
            await Task.Delay(_defaultCallTimeout);

            if (!_disposed && (DateTime.UtcNow >= _lastDataReceivedTime.Add(_defaultCallTimeout)))
            {
                // Dispose of the stream if a chunk isn't received within the defaultCallTimeout.
                var timeoutException = new TimeoutException("Did not receive any data in the alloted time.");
                await CompletePipeAndDisposeStream(timeoutException);
                // TODO: _runtime.RaiseUnhandledException(timeoutException);
            }
        }

        internal async Task CompletePipeAndDisposeStream(Exception? ex = null)
        {
            await _pipe.Writer.CompleteAsync(ex);
            Dispose(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _runtime.WebAssemblyJSDataStreamInstances.Remove(_streamId);
            }

            _disposed = true;
        }
    }
}

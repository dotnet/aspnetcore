// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal sealed class RemoteJSDataStream : Stream
    {
        private readonly RemoteJSRuntime _runtime;
        private readonly long _streamId;
        private readonly TimeSpan _jsInteropDefaultCallTimeout;
        private readonly long _totalLength;
        private readonly CancellationToken _streamCancellationToken;
        private readonly Stream _pipeReaderStream;
        private readonly Pipe _pipe;
        private long _bytesRead;
        private long _expectedChunkId;
        private DateTimeOffset _lastDataReceivedTime;
        private bool _disposed;

        public static async Task<bool> ReceiveData(RemoteJSRuntime runtime, long streamId, long chunkId, byte[] chunk, string error)
        {
            if (!runtime.RemoteJSDataStreamInstances.TryGetValue(streamId, out var instance))
            {
                // There is no data stream with the given identifier. It may have already been disposed.
                // We notify JS that the stream has been cancelled/disposed.
                return false;
            }

            return await instance.ReceiveData(chunkId, chunk, error);
        }

        public static async ValueTask<RemoteJSDataStream> CreateRemoteJSDataStreamAsync(
            RemoteJSRuntime runtime,
            IJSStreamReference jsStreamReference,
            long totalLength,
            long maximumIncomingBytes,
            TimeSpan jsInteropDefaultCallTimeout,
            long pauseIncomingBytesThreshold = -1,
            long resumeIncomingBytesThreshold = -1,
            CancellationToken cancellationToken = default)
        {
            // Enforce minimum 1 kb, maximum 50 kb, SignalR message size.
            // We budget 512 bytes overhead for the transfer, thus leaving at least 512 bytes for data
            // transfer per chunk with a 1 kb message size.
            // Additionally, to maintain interactivity, we put an upper limit of 50 kb on the message size.
            var chunkSize = maximumIncomingBytes > 1024 ?
                Math.Min(maximumIncomingBytes, 50*1024) - 512 :
                throw new ArgumentException($"SignalR MaximumIncomingBytes must be at least 1 kb.");

            var streamId = runtime.RemoteJSDataStreamNextInstanceId++;
            var remoteJSDataStream = new RemoteJSDataStream(runtime, streamId, totalLength, jsInteropDefaultCallTimeout, pauseIncomingBytesThreshold, resumeIncomingBytesThreshold, cancellationToken);
            await runtime.InvokeVoidAsync("Blazor._internal.sendJSDataStream", jsStreamReference, streamId, chunkSize);
            return remoteJSDataStream;
        }

        private RemoteJSDataStream(
            RemoteJSRuntime runtime,
            long streamId,
            long totalLength,
            TimeSpan jsInteropDefaultCallTimeout,
            long pauseIncomingBytesThreshold,
            long resumeIncomingBytesThreshold,
            CancellationToken cancellationToken)
        {
            _runtime = runtime;
            _streamId = streamId;
            _jsInteropDefaultCallTimeout = jsInteropDefaultCallTimeout;
            _totalLength = totalLength;
            _streamCancellationToken = cancellationToken;

            _lastDataReceivedTime = DateTimeOffset.UtcNow;
            _ = ThrowOnTimeout();

            _runtime.RemoteJSDataStreamInstances.Add(_streamId, this);

            _pipe = new Pipe(new PipeOptions(pauseWriterThreshold: pauseIncomingBytesThreshold, resumeWriterThreshold: resumeIncomingBytesThreshold));
            _pipeReaderStream = _pipe.Reader.AsStream();
            PipeReader = _pipe.Reader;
        }

        /// <summary>
        /// Gets a <see cref="PipeReader"/> to directly read data sent by the JavaScript client.
        /// </summary>
        public PipeReader PipeReader { get; }

        private async Task<bool> ReceiveData(long chunkId, byte[] chunk, string error)
        {
            try
            {
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
                    throw new EndOfStreamException("The incoming data chunk cannot be empty.");
                }

                _bytesRead += chunk.Length;

                if (_bytesRead > _totalLength)
                {
                    throw new EndOfStreamException($"The incoming data stream declared a length {_totalLength}, but {_bytesRead} bytes were sent.");
                }

                // Start timeout _after_ performing validations on data.
                _lastDataReceivedTime = DateTimeOffset.UtcNow;
                _ = ThrowOnTimeout();

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
                var timeoutException = new TimeoutException("Did not receive any data in the allotted time.");
                await CompletePipeAndDisposeStream(timeoutException);
                _runtime.RaiseUnhandledException(timeoutException);
            }
        }

        /// <summary>
        /// For testing purposes only.
        ///
        /// Triggers the timeout on the next check.
        /// </summary>
        internal void InvalidateLastDataReceivedTimeForTimeout()
        {
            _lastDataReceivedTime = _lastDataReceivedTime.Subtract(_jsInteropDefaultCallTimeout);
        }

        private async Task CompletePipeAndDisposeStream(Exception? ex = null)
        {
            await _pipe.Writer.CompleteAsync(ex);
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _runtime.RemoteJSDataStreamInstances.Remove(_streamId);
            }

            _disposed = true;
        }
    }
}

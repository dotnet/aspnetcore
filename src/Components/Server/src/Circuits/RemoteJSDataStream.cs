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
        private readonly long _totalLength;
        private readonly CancellationToken _streamCancellationToken;
        private readonly Timer _receiveDataTimer;
        private readonly Stream _pipeReaderStream;
        private readonly Pipe _pipe;
        private long _bytesRead;

        public static async Task<bool> ReceiveData(RemoteJSRuntime runtime, long streamId, byte[] chunk, string error)
        {
            if (!runtime.RemoteJSDataStreamInstances.TryGetValue(streamId, out var instance))
            {
                // There is no data stream with the given identifier. It may have already been disposed.
                // We notify JS that the stream has been cancelled/disposed.
                return false;
            }

            return await instance.ReceiveData(chunk, error);
        }

        public static async ValueTask<RemoteJSDataStream> CreateRemoteJSDataStreamAsync(
            RemoteJSRuntime runtime,
            IJSStreamReference jsStreamReference,
            long totalLength,
            long maxBufferSize,
            long maximumIncomingBytes,
            CancellationToken cancellationToken = default)
        {
            // Enforce minimum 1 kb SignalR message size as we budget 512 bytes
            // overhead for the transfer, thus leaving at least 512 bytes for data
            // transfer per chunk.
            var chunkSize = maximumIncomingBytes > 1024 ?
                maximumIncomingBytes - 512 :
                throw new ArgumentException($"SignalR MaximumIncomingBytes must be at least 1 kb.");

            var streamId = runtime.RemoteJSDataStreamNextInstanceId++;
            var remoteJSDataStream = new RemoteJSDataStream(runtime, streamId, totalLength, maxBufferSize, cancellationToken);
            await runtime.InvokeVoidAsync("Blazor._internal.sendJSDataStream", jsStreamReference, streamId, chunkSize);
            return remoteJSDataStream;
        }

        private RemoteJSDataStream(
            RemoteJSRuntime runtime,
            long streamId,
            long totalLength,
            long maxBufferSize,
            CancellationToken cancellationToken)
        {
            _runtime = runtime;
            _streamId = streamId;
            _totalLength = totalLength;
            _streamCancellationToken = cancellationToken;

            // Dispose of the stream if a chunk isn't received within 1 minute.
            _receiveDataTimer = new Timer(new TimerCallback(async (state) => {
                var stream = state as RemoteJSDataStream;
                var timeoutException = new TimeoutException("Did not receive any data in the alloted time.");
                await stream.CompletePipeAndDisposeStream(timeoutException);
            }), this, dueTime: ReceiveDataTimeout, period: Timeout.InfiniteTimeSpan);

            _runtime.RemoteJSDataStreamInstances.Add(_streamId, this);

            _pipe = new Pipe(new PipeOptions(pauseWriterThreshold: maxBufferSize, resumeWriterThreshold: maxBufferSize / 2));
            _pipeReaderStream = _pipe.Reader.AsStream();
        }

        // internal for testing
        internal TimeSpan ReceiveDataTimeout { private get; set; } = TimeSpan.FromMinutes(1);

        private async Task<bool> ReceiveData(byte[] chunk, string error)
        {
            try
            {
                // Reset the timeout as a chunk has been received
                _receiveDataTimer.Change(dueTime: ReceiveDataTimeout, period: Timeout.InfiniteTimeSpan);

                if (!string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException($"An error occurred while reading the remote stream: {error}");
                }

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

        internal async Task CompletePipeAndDisposeStream(Exception? ex = null)
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
        }
    }
}

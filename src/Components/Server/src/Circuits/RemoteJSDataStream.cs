// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class RemoteJSDataStream : Stream
    {
        // Concerns with static `Instances`? Malicious actor could hijack another user's
        // stream by (improbably) guessing the GUID. Maybe we put this in the JSRuntime for curcuit isolation?
        private readonly static Dictionary<Guid, RemoteJSDataStream> Instances = new();

        private readonly Guid _streamId;
        private readonly long _totalLength;
        private readonly CancellationToken _cancellationToken;
        private readonly Stream _pipeReaderStream;
        private readonly Pipe _pipe;
        private long _bytesRead;

        public static Task SupplyData(string streamId, ReadOnlySequence<byte> chunk, string error)
        {
            if (!Instances.TryGetValue(Guid.Parse(streamId), out var instance))
            {
                throw new InvalidOperationException("There is no data stream with the given identifier. It may have already been disposed.");
            }

            return instance.SupplyData(chunk, error);
        }

        public static async Task<RemoteJSDataStream> CreateRemoteJSDataStreamAsync(
            JSRuntime runtime,
            IJSDataReference jsDataReference,
            long totalLength,
            long maxBufferSize,
            CancellationToken cancellationToken = default)
        {
            var streamId = Guid.NewGuid();
            var remoteJSDataStream = new RemoteJSDataStream(streamId, totalLength, maxBufferSize, cancellationToken);
            await runtime.InvokeVoidAsync("Blazor._internal.sendJSDataStream", jsDataReference, streamId);
            return remoteJSDataStream;
        }

        private RemoteJSDataStream(Guid streamId, long totalLength, long maxBufferSize, CancellationToken cancellationToken)
        {
            _streamId = streamId;
            _totalLength = totalLength;
            _cancellationToken = cancellationToken;

            Instances.Add(_streamId, this);

            _pipe = new Pipe(new PipeOptions(pauseWriterThreshold: maxBufferSize, resumeWriterThreshold: maxBufferSize));
            _pipeReaderStream = _pipe.Reader.AsStream();
        }

        // TODO: Surely this should be IAsyncEnumerable<ReadOnlySequence<byte>> so we can pass through the
        // data without having to copy it into a temporary buffer. But trying this gives strange errors -
        // sometimes the "chunk" variable below has a negative length, even though the logic in BlazorPackHubProtocolWorker
        // never returns a corrupted item as far as I can tell.
        private async Task SupplyData(ReadOnlySequence<byte> chunk, string error)
        {
            if (chunk.Length < 0)
            {
                throw new InvalidOperationException($"The incoming data chunk cannot be negative.");
            }

            try
            {
                if (!string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException($"An error occurred while reading the remote stream: {error}");
                }

                if (chunk.Length == 0)
                {
                    throw new InvalidOperationException($"The incoming data chunk cannot be empty.");
                }

                _bytesRead += chunk.Length;

                if (_bytesRead > _totalLength)
                {
                    throw new InvalidOperationException($"The incoming data stream declared a length {_totalLength}, but {_bytesRead} bytes were read.");
                }

                // Enforce 1 MB max chunk size.
                const int maxChunkLength = 1024 * 1024;
                if (chunk.Length > maxChunkLength)
                {
                    throw new InvalidOperationException($"The incoming stream chunk of length {chunk.Length} exceeds the limit of {maxChunkLength}.");
                }

                CopyToPipeWriter(chunk, _pipe.Writer);
                _pipe.Writer.Advance((int)chunk.Length);
                await _pipe.Writer.FlushAsync(_cancellationToken);

                if (_bytesRead == _totalLength)
                {
                    await _pipe.Writer.CompleteAsync();
                }
            }
            catch (Exception e)
            {
                await _pipe.Writer.CompleteAsync(e);
                return;
            }
        }

        private static void CopyToPipeWriter(ReadOnlySequence<byte> chunk, PipeWriter writer)
        {
            var pipeBuffer = writer.GetSpan((int)chunk.Length);
            chunk.CopyTo(pipeBuffer);
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _pipeReaderStream.Length;

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
            return await _pipeReaderStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return await _pipeReaderStream.ReadAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Instances.Remove(_streamId);
            }
        }
    }
}

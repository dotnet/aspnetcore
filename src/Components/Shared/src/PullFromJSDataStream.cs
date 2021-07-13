// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components
{
    /// <Summary>
    /// A stream that pulls each chunk on demand using JavaScript interop. This implementation is used for
    /// WebAssembly and WebView applications.
    /// </Summary>
    internal sealed class PullFromJSDataStream : Stream
    {
        private readonly JSRuntime _runtime;
        private readonly IJSStreamReference _jsStreamReference;
        private readonly long _totalLength;
        private readonly CancellationToken _streamCancellationToken;
        private long _offset;

        public static PullFromJSDataStream CreateJSDataStream(
            JSRuntime runtime,
            IJSStreamReference jsStreamReference,
            long totalLength,
            CancellationToken cancellationToken = default)
        {
            var jsDataStream = new PullFromJSDataStream(runtime, jsStreamReference, totalLength, cancellationToken);
            return jsDataStream;
        }

        private PullFromJSDataStream(
            JSRuntime runtime,
            IJSStreamReference jsStreamReference,
            long totalLength,
            CancellationToken cancellationToken)
        {
            _runtime = runtime;
            _jsStreamReference = jsStreamReference;
            _totalLength = totalLength;
            _streamCancellationToken = cancellationToken;
            _offset = 0;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _totalLength;

        public override long Position
        {
            get => _offset;
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
            => await ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var bytesRead = await RequestDataFromJSAsync(buffer.Length);
            bytesRead.CopyTo(buffer);

            return bytesRead.Length;
        }

        private async ValueTask<byte[]> RequestDataFromJSAsync(int numBytesToRead)
        {
            numBytesToRead = (int)Math.Min(numBytesToRead, _totalLength - _offset);
            var bytesRead = await _runtime.InvokeAsync<byte[]>("Blazor._internal.getJSDataStreamChunk", _jsStreamReference, _offset, numBytesToRead);
            if (bytesRead.Length != numBytesToRead)
            {
                throw new EndOfStreamException($"Failed to read the requested number of bytes from the stream.");
            }

            _offset += bytesRead.Length;
            return bytesRead;
        }
    }
}

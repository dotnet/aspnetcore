// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components
{
    internal sealed class DefaultJSDataStream : BaseJSDataStream
    {
        private readonly JSRuntime _runtime;
        private readonly IJSStreamReference _jsStreamReference;
        private readonly CancellationToken _streamCancellationToken;
        private long _offset;

        internal static DefaultJSDataStream CreateJSDataStream(
            JSRuntime runtime,
            IJSStreamReference jsStreamReference,
            long totalLength,
            CancellationToken cancellationToken = default)
        {
            var webAssemblyJSDataStream = new DefaultJSDataStream(runtime, jsStreamReference, totalLength, cancellationToken);
            return webAssemblyJSDataStream;
        }

        private DefaultJSDataStream(
            JSRuntime runtime,
            IJSStreamReference jsStreamReference,
            long totalLength,
            CancellationToken cancellationToken) :
            base(totalLength)
        {
            _runtime = runtime;
            _jsStreamReference = jsStreamReference;
            _streamCancellationToken = cancellationToken;
            _offset = 0;
        }

        public override long Position
        {
            get => _offset;
            set => throw new NotSupportedException();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var numBytesToRead = Math.Min(count, buffer.Length - offset);
            var bytesRead = await RequestDataFromJSAsync(numBytesToRead);

            Array.Copy(sourceArray: bytesRead, sourceIndex: 0, destinationArray: buffer, destinationIndex: offset, length: bytesRead.Length);

            return bytesRead.Length;
        }

        private async Task<byte[]> RequestDataFromJSAsync(int numBytesToRead)
        {
            numBytesToRead = (int)Math.Min(numBytesToRead, _totalLength - _offset);
            var bytesRead = await _runtime.InvokeAsync<byte[]>("Blazor._internal.getJSDataStreamChunk", _jsStreamReference, _offset, numBytesToRead);
            _offset += bytesRead.Length;
            return bytesRead;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var bytesRead = await RequestDataFromJSAsync(buffer.Length);

            var bytesReadMemoryBuffer = new Memory<byte>(bytesRead);
            bytesReadMemoryBuffer.CopyTo(buffer);
            
            return bytesRead.Length;
        }
    }
}

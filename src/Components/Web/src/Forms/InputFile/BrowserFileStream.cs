// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Forms
{
    internal sealed class BrowserFileStream : Stream
    {
        private long _position;
        private readonly IJSRuntime _jsRuntime;
        private readonly ElementReference _inputFileElement;
        private readonly BrowserFile _file;
        private readonly long _maxAllowedSize;
        private readonly CancellationTokenSource _openReadStreamCts;
        private readonly Task<Stream> OpenReadStreamTask;

        private bool _isDisposed;
        private CancellationTokenSource? _copyFileDataCts;

        public BrowserFileStream(
            IJSRuntime jsRuntime,
            ElementReference inputFileElement,
            BrowserFile file,
            RemoteBrowserFileStreamOptions options,
            long maxAllowedSize,
            CancellationToken cancellationToken)
        {
            _jsRuntime = jsRuntime;
            _inputFileElement = inputFileElement;
            _file = file;
            _maxAllowedSize = maxAllowedSize;
            _openReadStreamCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            OpenReadStreamTask = OpenReadStreamAsync(options, _openReadStreamCts.Token);
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _file.Size;

        public override long Position
        {
            get => _position;
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

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var bytesAvailableToRead = Length - Position;
            var maxBytesToRead = (int)Math.Min(bytesAvailableToRead, buffer.Length);
            if (maxBytesToRead <= 0)
            {
                return 0;
            }

            var bytesRead = await CopyFileDataIntoBuffer(_position, buffer.Slice(0, maxBytesToRead), cancellationToken);

            _position += bytesRead;

            return bytesRead;
        }

        private async Task<Stream> OpenReadStreamAsync(RemoteBrowserFileStreamOptions options, CancellationToken cancellationToken)
        {
            var dataReference = await _jsRuntime.InvokeAsync<IJSStreamReference>(
                InputFileInterop.ReadFileData,
                cancellationToken,
                _inputFileElement,
                _file.Id);

            return await dataReference.OpenReadStreamAsync(
                _maxAllowedSize,
                cancellationToken: cancellationToken);
        }

        private async ValueTask<int> CopyFileDataIntoBuffer(long sourceOffset, Memory<byte> destination, CancellationToken cancellationToken)
        {
            var stream = await OpenReadStreamTask;
            _copyFileDataCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            return await stream.ReadAsync(destination, _copyFileDataCts.Token);
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            _openReadStreamCts.Cancel();
            _copyFileDataCts?.Cancel();

            _isDisposed = true;

            base.Dispose(disposing);
        }
    }
}

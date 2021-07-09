// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Forms
{
    internal class RemoteBrowserFileStream : BrowserFileStream
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ElementReference _inputFileElement;
        private readonly long _maxAllowedSize;
        private readonly CancellationTokenSource _openReadStreamCts;
        private readonly Task<Stream> OpenReadStreamTask;

        private bool _isDisposed;
        private CancellationTokenSource? _copyFileDataCts;

        public RemoteBrowserFileStream(
            IJSRuntime jsRuntime,
            ElementReference inputFileElement,
            BrowserFile file,
            RemoteBrowserFileStreamOptions options,
            long maxAllowedSize,
            CancellationToken cancellationToken)
            : base(file)
        {
            _jsRuntime = jsRuntime;
            _inputFileElement = inputFileElement;
            _maxAllowedSize = maxAllowedSize;
            _openReadStreamCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            OpenReadStreamTask = OpenReadStreamAsync(options, _openReadStreamCts.Token);
        }

        private async Task<Stream> OpenReadStreamAsync(RemoteBrowserFileStreamOptions options, CancellationToken cancellationToken)
        {
            var dataReference = await _jsRuntime.InvokeAsync<IJSStreamReference>(
                InputFileInterop.ReadFileData,
                cancellationToken,
                _inputFileElement,
                File.Id);

            return await dataReference.OpenReadStreamAsync(
                _maxAllowedSize,
                pauseIncomingBytesThreshold: options.MaxBufferSize,
                resumeIncomingBytesThreshold: options.MaxBufferSize / 2,
                cancellationToken);
        }

        protected override async ValueTask<int> CopyFileDataIntoBuffer(long sourceOffset, Memory<byte> destination, CancellationToken cancellationToken)
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

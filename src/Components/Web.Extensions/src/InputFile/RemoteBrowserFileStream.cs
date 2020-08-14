// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal class RemoteBrowserFileStream : BrowserFileStream
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ElementReference _inputFileElement;
        private readonly int _maxChunkSize;
        private readonly Memory<byte> _chunkBuffer;
        private readonly PipeReader _pipeReader;
        private readonly CancellationTokenSource _fillBufferCts;

        private bool _isReadingCompleted;
        private bool _isDisposed;

        public RemoteBrowserFileStream(IJSRuntime jsRuntime, ElementReference inputFileElement, int maxChunkSize, int maxBufferSize, BrowserFile file)
            : base(file)
        {
            _jsRuntime = jsRuntime;
            _inputFileElement = inputFileElement;
            _maxChunkSize = maxChunkSize;
            _chunkBuffer = new Memory<byte>(ArrayPool<byte>.Shared.Rent(_maxChunkSize));

            var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: maxBufferSize, resumeWriterThreshold: maxBufferSize));
            _pipeReader = pipe.Reader;
            _fillBufferCts = new CancellationTokenSource();

            _ = FillBuffer(pipe.Writer, _fillBufferCts.Token);
        }

        private async Task FillBuffer(PipeWriter writer, CancellationToken cancellationToken)
        {
            long offset = 0;

            while (offset < File.Size)
            {
                var pipeBuffer = writer.GetMemory(_maxChunkSize);
                var chunkSize = (int)Math.Min(_maxChunkSize, File.Size - offset);

                try
                {
                    var base64 = await _jsRuntime.InvokeAsync<string>(
                        InputFileInterop.ReadFileData,
                        cancellationToken,
                        _inputFileElement,
                        File.Id,
                        offset,
                        chunkSize);

                    if (!Convert.TryFromBase64String(base64, _chunkBuffer.Span, out var bytesWritten))
                    {
                        throw new FormatException("A chunk with an invalid format was received.");
                    }

                    if (bytesWritten != chunkSize)
                    {
                        throw new InvalidOperationException(
                            $"A chunk with size {bytesWritten} bytes was received, but {chunkSize} bytes were expected.");
                    }

                    _chunkBuffer.CopyTo(pipeBuffer);

                    writer.Advance(chunkSize);
                    offset += chunkSize;
                }
                catch (Exception e)
                {
                    await writer.CompleteAsync(e);
                    throw;
                }

                var result = await writer.FlushAsync();

                if (result.IsCompleted)
                {
                    break;
                }
            }

            await writer.CompleteAsync();
        }

        protected override async ValueTask<int> CopyFileDataIntoBuffer(long sourceOffset, Memory<byte> destination, CancellationToken cancellationToken)
        {
            if (_isReadingCompleted)
            {
                return 0;
            }

            int totalBytesCopied = 0;

            while (destination.Length > 0)
            {
                var result = await _pipeReader.ReadAsync(cancellationToken);

                if (result.IsCanceled)
                {
                    _isReadingCompleted = true;

                    var exception = new OperationCanceledException(cancellationToken);
                    await _pipeReader.CompleteAsync(exception);

                    throw exception;
                }

                var bytesToCopy = (int)Math.Min(result.Buffer.Length, destination.Length);
                var slice = result.Buffer.Slice(0, bytesToCopy);

                slice.CopyTo(destination.Span);
                _pipeReader.AdvanceTo(slice.End);

                totalBytesCopied += bytesToCopy;

                destination = destination.Slice(bytesToCopy);

                if (result.IsCompleted && slice.Length == 0)
                {
                    _isReadingCompleted = true;

                    await _pipeReader.CompleteAsync();

                    break;
                }
            }

            return totalBytesCopied;
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            _fillBufferCts.Cancel();

            if (disposing)
            {
                _fillBufferCts.Dispose();
            }

            _isDisposed = true;

            base.Dispose(disposing);
        }
    }
}

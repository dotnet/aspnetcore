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
        private readonly PipeReader _pipeReader;
        private readonly CancellationTokenSource _fillBufferCts;
        private readonly TimeSpan _chunkFetchTimeout;

        private bool _isReadingCompleted;
        private bool _isDisposed;

        public RemoteBrowserFileStream(
            IJSRuntime jsRuntime,
            ElementReference inputFileElement,
            BrowserFile file,
            RemoteBrowserFileStreamOptions options,
            CancellationToken cancellationToken)
            : base(file)
        {
            _jsRuntime = jsRuntime;
            _inputFileElement = inputFileElement;
            _maxChunkSize = options.ChunkSize;
            _chunkFetchTimeout = options.ChunkFetchTimeout;

            var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: options.MaxBufferSize, resumeWriterThreshold: options.MaxBufferSize));
            _pipeReader = pipe.Reader;
            _fillBufferCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

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
                    using var readChunkCts = CancellationTokenSource.CreateLinkedTokenSource(_fillBufferCts.Token);
                    readChunkCts.CancelAfter(_chunkFetchTimeout);

                    var base64 = await _jsRuntime.InvokeAsync<string>(
                        InputFileInterop.ReadFileData,
                        readChunkCts.Token,
                        _inputFileElement,
                        File.Id,
                        offset,
                        chunkSize);

                    if (!Convert.TryFromBase64String(base64, pipeBuffer.Span, out var bytesWritten))
                    {
                        throw new FormatException("A chunk with an invalid format was received.");
                    }

                    if (bytesWritten != chunkSize)
                    {
                        throw new InvalidOperationException(
                            $"A chunk with size {bytesWritten} bytes was received, but {chunkSize} bytes were expected.");
                    }

                    writer.Advance(chunkSize);
                    offset += chunkSize;

                    var result = await writer.FlushAsync(cancellationToken);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    await writer.CompleteAsync(e);
                    return;
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

            _isDisposed = true;

            base.Dispose(disposing);
        }
    }
}

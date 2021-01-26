// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Forms
{
    internal class RemoteBrowserFileStream : BrowserFileStream
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ElementReference _inputFileElement;
        private readonly int _maxSegmentSize;
        private readonly PipeReader _pipeReader;
        private readonly CancellationTokenSource _fillBufferCts;
        private readonly TimeSpan _segmentFetchTimeout;

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
            _maxSegmentSize = options.MaxSegmentSize;
            _segmentFetchTimeout = options.SegmentFetchTimeout;

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
                var pipeBuffer = writer.GetMemory(_maxSegmentSize);
                var segmentSize = (int)Math.Min(_maxSegmentSize, File.Size - offset);

                try
                {
                    using var readSegmentCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    readSegmentCts.CancelAfter(_segmentFetchTimeout);

                    var bytes = await _jsRuntime.InvokeAsync<byte[]>(
                        InputFileInterop.ReadFileData,
                        readSegmentCts.Token,
                        _inputFileElement,
                        File.Id,
                        offset,
                        segmentSize);

                    if (bytes is null || bytes.Length != segmentSize)
                    {
                        throw new InvalidOperationException(
                            $"A segment with size {bytes?.Length ?? 0} bytes was received, but {segmentSize} bytes were expected.");
                    }

                    bytes.CopyTo(pipeBuffer);
                    writer.Advance(segmentSize);
                    offset += segmentSize;

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
                var bytesToCopy = (int)Math.Min(result.Buffer.Length, destination.Length);

                if (bytesToCopy == 0)
                {
                    if (result.IsCompleted)
                    {
                        _isReadingCompleted = true;
                        await _pipeReader.CompleteAsync();
                    }

                    break;
                }

                var slice = result.Buffer.Slice(0, bytesToCopy);
                slice.CopyTo(destination.Span);

                _pipeReader.AdvanceTo(slice.End);

                totalBytesCopied += bytesToCopy;
                destination = destination.Slice(bytesToCopy);
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

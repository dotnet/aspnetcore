// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        private readonly PreFetchingSequence _chunkSequence;
        private readonly byte[] _currentChunkDecodingBuffer;

        private EncodedFileChunk? _currentChunk;
        private int _currentChunkDecodingBufferConsumedLength;

        public RemoteBrowserFileStream(IJSRuntime jsRuntime, ElementReference inputFileElement, int maxChunkSize, int maxBufferSize, BrowserFile file)
            : base(file)
        {
            _jsRuntime = jsRuntime;
            _inputFileElement = inputFileElement;
            _maxChunkSize = maxChunkSize;
            _chunkSequence = new PreFetchingSequence(
                FetchEncodedChunk,
                (File.Size + _maxChunkSize - 1) / _maxChunkSize,
                Math.Max(1, maxBufferSize / _maxChunkSize)); // Degree of parallelism on fetch.
            _currentChunkDecodingBuffer = new byte[_maxChunkSize];
        }

        protected override async ValueTask<int> CopyFileDataIntoBuffer(long sourceOffset, byte[] destination, int destinationOffset, int maxBytes, CancellationToken cancellationToken)
        {
            var totalBytesCopied = 0;

            while (maxBytes > 0)
            {
                // If we don't yet have a chunk, or it's fully consumed, get the next one.
                if (!_currentChunk.HasValue || _currentChunkDecodingBufferConsumedLength == _currentChunk.Value.LengthBytes)
                {
                    // If we've already read some data, and the next chunk is still pending,
                    // then just return now rather than awaiting.
                    if (totalBytesCopied > 0 && _chunkSequence.TryPeekNext(out var nextChunk) && !nextChunk.Base64.IsCompleted)
                    {
                        break;
                    }

                    _currentChunk = _chunkSequence.ReadNext(cancellationToken);

                    var currentEncodedChunk = await _currentChunk.Value.Base64;

                    DecodeChunkToBuffer(currentEncodedChunk, _currentChunkDecodingBuffer, 0, _currentChunk.Value.LengthBytes);

                    _currentChunkDecodingBufferConsumedLength = 0;
                }

                // How much of the current chunk can we fit into the destination?
                var numUnconsumedBytesInChunk = _currentChunk.Value.LengthBytes - _currentChunkDecodingBufferConsumedLength;
                var numBytesToTransfer = Math.Min(numUnconsumedBytesInChunk, maxBytes);

                if (numBytesToTransfer == 0)
                {
                    break;
                }

                // Perform the copy.
                Array.Copy(_currentChunkDecodingBuffer, _currentChunkDecodingBufferConsumedLength, destination, destinationOffset, numBytesToTransfer);
                maxBytes -= numBytesToTransfer;
                destinationOffset += numBytesToTransfer;
                _currentChunkDecodingBufferConsumedLength += numBytesToTransfer;
                totalBytesCopied += numBytesToTransfer;
            }

            return totalBytesCopied;
        }

        private EncodedFileChunk FetchEncodedChunk(long index, CancellationToken cancellationToken)
        {
            var sourceOffset = index * _maxChunkSize;
            var chunkLength = (int)Math.Min(_maxChunkSize, File.Size - sourceOffset);
            var task = _jsRuntime.InvokeAsync<string>(
                InputFileInterop.ReadFileData,
                cancellationToken,
                _inputFileElement,
                File.Id,
                index * _maxChunkSize,
                chunkLength).AsTask();

            return new EncodedFileChunk(task, chunkLength);
        }

        private int DecodeChunkToBuffer(string base64, byte[] buffer, int offset, int maxBytesToRead)
        {
            var bytes = Convert.FromBase64String(base64);

            if (bytes.Length > maxBytesToRead)
            {
                throw new InvalidOperationException($"Requested a maximum of {maxBytesToRead} bytes, but received {bytes.Length}.");
            }

            Array.Copy(bytes, 0, buffer, offset, bytes.Length);

            return bytes.Length;
        }
    }
}

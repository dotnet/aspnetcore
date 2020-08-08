// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal class RemoteFileListEntryStream : FileListEntryStream
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ElementReference _inputFileElement;
        private readonly int _maxMessageSize;
        private readonly PreFetchingSequence _blockSequence;

        private Block? _currentBlock;
        private byte[] _currentBlockDecodingBuffer;
        private int _currentBlockDecodingBufferConsumedLength;

        public RemoteFileListEntryStream(IJSRuntime jsRuntime, ElementReference inputFileElement, int maxMessageSize, int maxBufferSize, FileListEntry file)
            : base(file)
        {
            _jsRuntime = jsRuntime;
            _inputFileElement = inputFileElement;
            _maxMessageSize = maxMessageSize;
            _blockSequence = new PreFetchingSequence(
                FetchBase64Block,
                (File.Size + _maxMessageSize - 1) / _maxMessageSize,
                Math.Max(1, maxBufferSize / _maxMessageSize)); // Degree of parallelism on fetch.
            _currentBlockDecodingBuffer = new byte[_maxMessageSize];
        }

        protected override async Task<int> CopyFileDataIntoBuffer(long sourceOffset, byte[] destination, int destinationOffset, int maxBytes, CancellationToken cancellationToken)
        {
            var totalBytesCopied = 0;

            while (maxBytes > 0)
            {
                // If we don't yet have a block, or it's fully consumed, get the next one.
                if (!_currentBlock.HasValue || _currentBlockDecodingBufferConsumedLength == _currentBlock.Value.LengthBytes)
                {
                    // If we've already read some data, and the next block is still pending,
                    // then just return now rather than awaiting.
                    if (totalBytesCopied > 0 && _blockSequence.TryPeekNext(out var nextBlock) && !nextBlock.Base64.IsCompleted)
                    {
                        break;
                    }

                    _currentBlock = _blockSequence.ReadNext(cancellationToken);

                    var currentBlockBase64 = await _currentBlock.Value.Base64;

                    DecodeBase64ToBuffer(currentBlockBase64, _currentBlockDecodingBuffer, 0, _currentBlock.Value.LengthBytes);

                    _currentBlockDecodingBufferConsumedLength = 0;
                }

                // How much of the current block can we fit into the destination?
                var numUnconsumedBytesInBlock = _currentBlock.Value.LengthBytes - _currentBlockDecodingBufferConsumedLength;
                var numBytesToTransfer = Math.Min(numUnconsumedBytesInBlock, maxBytes);

                if (numBytesToTransfer == 0)
                {
                    break;
                }

                // Perform the copy.
                Array.Copy(_currentBlockDecodingBuffer, _currentBlockDecodingBufferConsumedLength, destination, destinationOffset, numBytesToTransfer);
                maxBytes -= numBytesToTransfer;
                destinationOffset += numBytesToTransfer;
                _currentBlockDecodingBufferConsumedLength += numBytesToTransfer;
                totalBytesCopied += numBytesToTransfer;
            }

            return totalBytesCopied;
        }

        private Block FetchBase64Block(long index, CancellationToken cancellationToken)
        {
            var sourceOffset = index * _maxMessageSize;
            var blockLength = (int)Math.Min(_maxMessageSize, File.Size - sourceOffset);
            var task = _jsRuntime.InvokeAsync<string>(
                InputFileInterop.ReadFileData,
                cancellationToken,
                _inputFileElement,
                File.Id,
                index * _maxMessageSize,
                blockLength).AsTask();

            return new Block(task, blockLength);
        }

        private int DecodeBase64ToBuffer(string base64, byte[] buffer, int offset, int maxBytesToRead)
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

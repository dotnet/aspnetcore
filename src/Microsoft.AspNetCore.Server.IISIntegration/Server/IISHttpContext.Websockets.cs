// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.HttpSys.Internal;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    /// <summary>
    /// Represents the websocket portion of the <see cref="IISHttpContext"/>
    /// </summary>
    internal partial class IISHttpContext
    {
        private bool _wasUpgraded; // Used for detecting repeated upgrades in IISHttpContext

        private IISAwaitable _readWebSocketsOperation;
        private IISAwaitable _writeWebSocketsOperation;
        private TaskCompletionSource<object> _upgradeTcs;

        private Task StartBidirectionalStream()
        {
            // IIS allows for websocket support and duplex channels only on Win8 and above
            // This allows us to have two tasks for reading the request and writing the response
            var readWebsocketTask = ReadWebSockets();
            var writeWebsocketTask = WriteWebSockets();
            return Task.WhenAll(readWebsocketTask, writeWebsocketTask);
        }

        public async Task UpgradeAsync()
        {
            if (_upgradeTcs == null)
            {
                _upgradeTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                // Flush any contents of the OutputPipe before upgrading to websockets.
                await FlushAsync();
                await _upgradeTcs.Task;
            }
        }

        private unsafe IISAwaitable ReadWebSocketsFromIISAsync(int length)
        {
            var hr = 0;
            int dwReceivedBytes;
            bool fCompletionExpected;

            // For websocket calls, we can directly provide a callback function to be called once the websocket operation completes.
            hr = NativeMethods.HttpWebsocketsReadBytes(
                                        _pInProcessHandler,
                                        (byte*)_inputHandle.Pointer,
                                        length,
                                        IISAwaitable.ReadCallback,
                                        (IntPtr)_thisHandle,
                                        out dwReceivedBytes,
                                        out fCompletionExpected);
            if (!fCompletionExpected)
            {
                CompleteReadWebSockets(hr, dwReceivedBytes);
            }

            return _readWebSocketsOperation;
        }

        private unsafe IISAwaitable WriteWebSocketsFromIISAsync(ReadOnlySequence<byte> buffer)
        {
            var fCompletionExpected = false;
            var hr = 0;
            var nChunks = 0;

            if (buffer.IsSingleSegment)
            {
                nChunks = 1;
            }
            else
            {
                foreach (var memory in buffer)
                {
                    nChunks++;
                }
            }

            if (buffer.IsSingleSegment)
            {
                var pDataChunks = stackalloc HttpApiTypes.HTTP_DATA_CHUNK[1];

                fixed (byte* pBuffer = &MemoryMarshal.GetReference(buffer.First.Span))
                {
                    ref var chunk = ref pDataChunks[0];

                    chunk.DataChunkType = HttpApiTypes.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    chunk.fromMemory.pBuffer = (IntPtr)pBuffer;
                    chunk.fromMemory.BufferLength = (uint)buffer.Length;
                    hr = NativeMethods.HttpWebsocketsWriteBytes(_pInProcessHandler, pDataChunks, nChunks, IISAwaitable.WriteCallback, (IntPtr)_thisHandle, out fCompletionExpected);
                }
            }
            else
            {
                // REVIEW: Do we need to guard against this getting too big? It seems unlikely that we'd have more than say 10 chunks in real life
                var pDataChunks = stackalloc HttpApiTypes.HTTP_DATA_CHUNK[nChunks];
                var currentChunk = 0;

                // REVIEW: We don't really need this list since the memory is already pinned with the default pool,
                // but shouldn't assume the pool implementation right now. Unfortunately, this causes a heap allocation...
                var handles = new MemoryHandle[nChunks];

                foreach (var b in buffer)
                {
                    ref var handle = ref handles[currentChunk];
                    ref var chunk = ref pDataChunks[currentChunk];

                    handle = b.Pin();

                    chunk.DataChunkType = HttpApiTypes.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    chunk.fromMemory.BufferLength = (uint)b.Length;
                    chunk.fromMemory.pBuffer = (IntPtr)handle.Pointer;

                    currentChunk++;
                }

                hr = NativeMethods.HttpWebsocketsWriteBytes(_pInProcessHandler, pDataChunks, nChunks, IISAwaitable.WriteCallback, (IntPtr)_thisHandle, out fCompletionExpected);

                foreach (var handle in handles)
                {
                    handle.Dispose();
                }
            }

            if (!fCompletionExpected)
            {
                CompleteWriteWebSockets(hr, 0);
            }

            return _writeWebSocketsOperation;
        }

        internal void CompleteWriteWebSockets(int hr, int cbBytes)
        {
            _writeWebSocketsOperation.Complete(hr, cbBytes);
        }

        internal void CompleteReadWebSockets(int hr, int cbBytes)
        {
            _readWebSocketsOperation.Complete(hr, cbBytes);
        }

        private async Task ReadWebSockets()
        {
            try
            {
                while (true)
                {
                    var memory = Input.Writer.GetMemory();
                    _inputHandle = memory.Pin();

                    try
                    {
                        int read = 0;
                        read = await ReadWebSocketsFromIISAsync(memory.Length);

                        if (read == 0)
                        {
                            break;
                        }

                        Input.Writer.Advance(read);
                    }
                    finally
                    {
                        _inputHandle.Dispose();
                    }

                    var result = await Input.Writer.FlushAsync();

                    if (result.IsCompleted || result.IsCanceled)
                    {
                        break;
                    }
                }
                Input.Writer.Complete();
            }
            catch (Exception ex)
            {
                Input.Writer.Complete(ex);
            }
        }

        private async Task WriteWebSockets()
        {
            try
            {
                while (true)
                {
                    var result = await Output.Reader.ReadAsync();

                    var buffer = result.Buffer;
                    var consumed = buffer.End;

                    try
                    {
                        if (!buffer.IsEmpty)
                        {
                            await WriteWebSocketsFromIISAsync(buffer);
                        }
                        else if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        Output.Reader.AdvanceTo(consumed);
                    }
                }

                Output.Reader.Complete();
            }
            catch (Exception ex)
            {
                Output.Reader.Complete(ex);
            }
        }
    }
}

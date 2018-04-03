// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.HttpSys.Internal;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal partial class IISHttpContext
    {
        private const int HttpDataChunkStackLimit = 128; // 16 bytes per HTTP_DATA_CHUNK

        /// <summary>
        /// Reads data from the Input pipe to the user.
        /// </summary>
        /// <param name="memory"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal async Task<int> ReadAsync(Memory<byte> memory, CancellationToken cancellationToken)
        {
            StartProcessingRequestAndResponseBody();

            while (true)
            {
                var result = await Input.Reader.ReadAsync();
                var readableBuffer = result.Buffer;
                try
                {
                    if (!readableBuffer.IsEmpty)
                    {
                        var actual = Math.Min(readableBuffer.Length, memory.Length);
                        readableBuffer = readableBuffer.Slice(0, actual);
                        readableBuffer.CopyTo(memory.Span);
                        return (int)actual;
                    }
                    else if (result.IsCompleted)
                    {
                        return 0;
                    }
                }
                finally
                {
                    Input.Reader.AdvanceTo(readableBuffer.End, readableBuffer.End);
                }
            }
        }

        /// <summary>
        /// Writes data to the output pipe.
        /// </summary>
        /// <param name="memory"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal Task WriteAsync(ReadOnlyMemory<byte> memory, CancellationToken cancellationToken = default(CancellationToken))
        {

            // Want to keep exceptions consistent,
            if (!_hasResponseStarted)
            {
                return WriteAsyncAwaited(memory, cancellationToken);
            }

            lock (_stateSync)
            {
                DisableReads();
                return Output.WriteAsync(memory, cancellationToken);
            }
        }

        /// <summary>
        /// Flushes the data in the output pipe
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_hasResponseStarted)
            {
                return FlushAsyncAwaited(cancellationToken);
            }
            lock (_stateSync)
            {
                DisableReads();
                return Output.FlushAsync(cancellationToken);
            }
        }

        private void StartProcessingRequestAndResponseBody()
        {
            if (_processBodiesTask == null)
            {
                lock (_createReadWriteBodySync)
                {
                    if (_processBodiesTask == null)
                    {
                        _processBodiesTask = ConsumeAsync();
                    }
                }
            }
        }

        private async Task FlushAsyncAwaited(CancellationToken cancellationToken)
        {
            await InitializeResponseAwaited();

            Task flushTask;
            lock (_stateSync)
            {
                DisableReads();

                // Want to guarantee that data has been written to the pipe before releasing the lock.
                flushTask = Output.FlushAsync(cancellationToken: cancellationToken);
            }
            await flushTask;
        }

        private async Task WriteAsyncAwaited(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            // WriteAsyncAwaited is only called for the first write to the body.
            // Ensure headers are flushed if Write(Chunked)Async isn't called.
            await InitializeResponseAwaited();

            Task writeTask;
            lock (_stateSync)
            {
                DisableReads();

                // Want to guarantee that data has been written to the pipe before releasing the lock.
                writeTask = Output.WriteAsync(data, cancellationToken: cancellationToken);
            }
            await writeTask;
        }

        // ConsumeAsync is called when either the first read or first write is done.
        // There are two modes for reading and writing to the request/response bodies without upgrade.
        // 1. Await all reads and try to read from the Output pipe
        // 2. Done reading and await all writes.
        // If the request is upgraded, we will start bidirectional streams for the input and output.
        private async Task ConsumeAsync()
        {
            await ReadAndWriteLoopAsync();

            // The ReadAndWriteLoop can return due to being upgraded. Check if _wasUpgraded is true to determine
            // whether we go to a bidirectional stream or only write.
            if (_wasUpgraded)
            {
                await StartBidirectionalStream();
            }
        }

        private unsafe IISAwaitable ReadFromIISAsync(int length)
        {
            Action completion = null;
            lock (_stateSync)
            {
                // We don't want to read if there is data available in the output pipe
                // Therefore, we mark the current operation as cancelled to allow for the read
                // to be requeued.
                if (Output.Reader.TryRead(out var result))
                {
                    // If the buffer is empty, it is considered a write of zero.
                    // we still want to cancel and allow the write to occur.
                    completion = _operation.GetCompletion(hr: IISServerConstants.HResultCancelIO, cbBytes: 0);
                    Output.Reader.AdvanceTo(result.Buffer.Start);
                }
                else
                {
                    var hr = NativeMethods.HttpReadRequestBytes(
                           _pInProcessHandler,
                           (byte*)_inputHandle.Pointer,
                           length,
                           out var dwReceivedBytes,
                           out bool fCompletionExpected);
                    // if we complete the read synchronously, there is no need to set the reading flag
                    // as there is no cancelable operation.
                    if (!fCompletionExpected)
                    {
                        completion = _operation.GetCompletion(hr, dwReceivedBytes);
                    }
                    else
                    {
                        _reading = true;
                    }
                }
            }

            // Invoke the completion outside of the lock if the reead finished synchronously.
            completion?.Invoke();

            return _operation;
        }

        private unsafe IISAwaitable WriteToIISAsync(ReadOnlySequence<byte> buffer)
        {
            var fCompletionExpected = false;
            var hr = 0;
            var nChunks = 0;

            // Count the number of chunks in memory.
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

            if (nChunks == 1)
            {
                // If there is only a single chunk, use fixed to get a pointer to the buffer
                var pDataChunks = stackalloc HttpApiTypes.HTTP_DATA_CHUNK[1];

                fixed (byte* pBuffer = &MemoryMarshal.GetReference(buffer.First.Span))
                {
                    ref var chunk = ref pDataChunks[0];

                    chunk.DataChunkType = HttpApiTypes.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    chunk.fromMemory.pBuffer = (IntPtr)pBuffer;
                    chunk.fromMemory.BufferLength = (uint)buffer.Length;
                    hr = NativeMethods.HttpWriteResponseBytes(_pInProcessHandler, pDataChunks, nChunks, out fCompletionExpected);
                }
            }
            else if (nChunks < HttpDataChunkStackLimit)
            {
                // To avoid stackoverflows, we will only stackalloc if the write size is less than the StackChunkLimit
                // The stack size is IIS is by default 128/256 KB, so we are generous with this threshold.
                var pDataChunks = stackalloc HttpApiTypes.HTTP_DATA_CHUNK[nChunks];
                hr = WriteSequenceToIIS(nChunks, buffer, pDataChunks, out fCompletionExpected);
            }
            else
            {
                // Otherwise allocate the chunks on the heap.
                var chunks = new HttpApiTypes.HTTP_DATA_CHUNK[nChunks];
                fixed (HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks = chunks)
                {
                    hr = WriteSequenceToIIS(nChunks, buffer, pDataChunks, out fCompletionExpected);
                }
            }

            if (!fCompletionExpected)
            {
                _operation.Complete(hr, 0);
            }
            return _operation;
        }

        private unsafe int WriteSequenceToIIS(int nChunks, ReadOnlySequence<byte> buffer, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, out bool fCompletionExpected)
        {
            var currentChunk = 0;
            var hr = 0;

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

            hr = NativeMethods.HttpWriteResponseBytes(_pInProcessHandler, pDataChunks, nChunks, out fCompletionExpected);

            // Free the handles
            foreach (var handle in handles)
            {
                handle.Dispose();
            }

            return hr;
        }

        private unsafe IISAwaitable FlushToIISAsync()
        {
            // Calls flush
            var hr = 0;
            hr = NativeMethods.HttpFlushResponseBytes(_pInProcessHandler, out var fCompletionExpected);
            if (!fCompletionExpected)
            {
                _operation.Complete(hr, 0);
            }

            return _operation;
        }

        /// <summary>
        /// Main function for control flow with IIS.
        /// Uses two Pipes (Input and Output) between application calls to Read/Write/FlushAsync
        /// Control Flow:
        /// Try to see if there is data written by the application code (using TryRead)
        /// and write it to IIS.
        /// Check if the connection has been upgraded and call StartBidirectionalStreams
        /// if it has.
        /// Await reading from IIS, which will be cancelled if application code calls Write/FlushAsync.
        /// </summary>
        /// <returns>The Reading and Writing task.</returns>
        private async Task ReadAndWriteLoopAsync()
        {
            try
            {
                while (true)
                {
                    // First we check if there is anything to write from the Output pipe
                    // If there is, we call WriteToIISAsync
                    // Check if Output pipe has anything to write to IIS.
                    if (Output.Reader.TryRead(out var readResult))
                    {
                        var buffer = readResult.Buffer;

                        try
                        {
                            if (!buffer.IsEmpty)
                            {
                                // Write to IIS buffers
                                // Guaranteed to write the entire buffer to IIS
                                await WriteToIISAsync(buffer);
                            }
                            else if (readResult.IsCompleted)
                            {
                                break;
                            }
                            else
                            {
                                // Flush of zero bytes
                                await FlushToIISAsync();
                            }
                        }
                        finally
                        {
                            // Always Advance the data pointer to the end of the buffer.
                            Output.Reader.AdvanceTo(buffer.End);
                        }
                    }

                    // Check if there was an upgrade. If there is, we will replace the request and response bodies with
                    // two seperate loops. These will still be using the same Input and Output pipes here.
                    if (_upgradeTcs?.TrySetResult(null) == true)
                    {
                        // _wasUpgraded will be set at this point, exit the loop and we will check if we upgraded or not
                        // when going to next read/write type.
                        return;
                    }

                    // Now we handle the read.
                    var memory = Input.Writer.GetMemory();
                    _inputHandle = memory.Pin();

                    try
                    {
                        // Lock around invoking ReadFromIISAsync as we don't want to call CancelIo
                        // when calling read
                        var read = await ReadFromIISAsync(memory.Length);

                        // read value of 0 == done reading
                        // read value of -1 == read cancelled, still allowed to read but we
                        // need a write to occur first.
                        if (read == 0)
                        {
                            break;
                        }
                        else if (read == -1)
                        {
                            continue;
                        }
                        Input.Writer.Advance(read);
                    }
                    finally
                    {
                        // Always commit any changes to the Input pipe
                        _inputHandle.Dispose();
                    }

                    // Flush the read data for the Input Pipe writer
                    var flushResult = await Input.Writer.FlushAsync();

                    // If the pipe was closed, we are done reading,
                    if (flushResult.IsCompleted || flushResult.IsCanceled)
                    {
                        break;
                    }
                }

                // Complete the input writer as we are done reading the request body.
                Input.Writer.Complete();
            }
            catch (Exception ex)
            {
                Input.Writer.Complete(ex);
            }

            await WriteLoopAsync();
        }

        /// <summary>
        /// Secondary function for control flow with IIS. This is only called once we are done
        /// reading the request body. We now await reading from the Output pipe.
        /// </summary>
        /// <returns></returns>
        private async Task WriteLoopAsync()
        {
            try
            {
                while (true)
                {
                    // Reading is done, so we will await all reads from the output pipe
                    var readResult = await Output.Reader.ReadAsync();

                    // Get data from pipe
                    var buffer = readResult.Buffer;

                    try
                    {
                        if (!buffer.IsEmpty)
                        {
                            // Write to IIS buffers
                            // Guaranteed to write the entire buffer to IIS
                            await WriteToIISAsync(buffer);
                        }
                        else if (readResult.IsCompleted)
                        {
                            break;
                        }
                        else
                        {
                            // Flush of zero bytes will
                            await FlushToIISAsync();
                        }
                    }
                    finally
                    {
                        // Always Advance the data pointer to the end of the buffer.
                        Output.Reader.AdvanceTo(buffer.End);
                    }
                }

                // Close the output pipe as we are done reading from it.
                Output.Reader.Complete();
            }
            catch (Exception ex)
            {
                Output.Reader.Complete(ex);
            }
        }

        // Always called from within a lock
        private void DisableReads()
        {
            // To avoid concurrent reading and writing, if we have a pending read,
            // we must cancel it.
            // _reading will always be false if we upgrade to websockets, so we don't need to check wasUpgrade
            // Also, we set _reading to false after cancelling to detect redundant calls
            if (_reading)
            {
                _reading = false;
                // Calls IHttpContext->CancelIo(), which will cause the OnAsyncCompletion handler to fire.
                NativeMethods.HttpTryCancelIO(_pInProcessHandler);
            }
        }
    }
}

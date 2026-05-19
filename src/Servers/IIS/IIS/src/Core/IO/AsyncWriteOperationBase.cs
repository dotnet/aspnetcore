// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.Server.IIS.Core.IO;

internal abstract class AsyncWriteOperationBase : AsyncIOOperation
{
    private const int HttpDataChunkStackLimit = 128; // 16 bytes per HTTP_DATA_CHUNK

    private NativeSafeHandle? _requestHandler;
    private ReadOnlySequence<byte> _buffer;
    private MemoryHandle[]? _handles;

    public void Initialize(NativeSafeHandle requestHandler, ReadOnlySequence<byte> buffer)
    {
        _requestHandler = requestHandler;
        _buffer = buffer;
    }

    protected override unsafe bool InvokeOperation(out int hr, out int bytes)
    {
        Debug.Assert(_requestHandler != null, "Must initialize first.");

        if (_buffer.Length > int.MaxValue)
        {
            throw new InvalidOperationException($"Writes larger then {int.MaxValue} are not supported.");
        }

        bool completionExpected;
        var chunkCount = GetChunkCount();

        var bufferLength = (int)_buffer.Length;

        if (chunkCount < HttpDataChunkStackLimit)
        {
            // To avoid stackoverflows, we will only stackalloc if the write size is less than the StackChunkLimit
            // The stack size is IIS is by default 128/256 KB, so we are generous with this threshold.
            var chunks = stackalloc HTTP_DATA_CHUNK[chunkCount];
            hr = WriteSequence(_requestHandler, chunkCount, _buffer, chunks, out completionExpected);
        }
        else
        {
            // Otherwise allocate the chunks on the heap.
            var chunks = new HTTP_DATA_CHUNK[chunkCount];
            fixed (HTTP_DATA_CHUNK* pDataChunks = chunks)
            {
                hr = WriteSequence(_requestHandler, chunkCount, _buffer, pDataChunks, out completionExpected);
            }
        }

        bytes = bufferLength;
        return !completionExpected;
    }

    public override void FreeOperationResources(int hr, int bytes)
    {
        if (_handles != null)
        {
            // Free the handles
            foreach (var handle in _handles)
            {
                handle.Dispose();
            }
        }
    }

    protected override void ResetOperation()
    {
        base.ResetOperation();

        _requestHandler = default;
        _buffer = default;
        _handles.AsSpan().Clear();
    }

    private int GetChunkCount()
    {
        if (_buffer.IsSingleSegment)
        {
            return 1;
        }

        var count = 0;

        foreach (var _ in _buffer)
        {
            count++;
        }

        return count;
    }

    private unsafe int WriteSequence(NativeSafeHandle requestHandler, int nChunks, ReadOnlySequence<byte> buffer, HTTP_DATA_CHUNK* pDataChunks, out bool fCompletionExpected)
    {
        var currentChunk = 0;

        if (_handles == null || _handles.Length < nChunks)
        {
            _handles = new MemoryHandle[nChunks];
        }

        foreach (var readOnlyMemory in buffer)
        {
            ref var handle = ref _handles[currentChunk];
            ref var chunk = ref pDataChunks[currentChunk];
            handle = readOnlyMemory.Pin();

            chunk.DataChunkType = HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
            chunk.Anonymous.FromMemory.BufferLength = (uint)readOnlyMemory.Length;
            chunk.Anonymous.FromMemory.pBuffer = handle.Pointer;

            currentChunk++;
        }

        return WriteChunks(requestHandler, nChunks, pDataChunks, out fCompletionExpected);
    }

    protected abstract unsafe int WriteChunks(NativeSafeHandle requestHandler, int chunkCount, HTTP_DATA_CHUNK* dataChunks, out bool completionExpected);
}

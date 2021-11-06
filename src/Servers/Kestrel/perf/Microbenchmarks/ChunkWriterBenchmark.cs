// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class ChunkWriterBenchmark
{
    private const int InnerLoopCount = 1024;

    private PipeReader _reader;
    private PipeWriter _writer;
    private MemoryPool<byte> _memoryPool;

    [GlobalSetup]
    public void Setup()
    {
        _memoryPool = PinnedBlockMemoryPoolFactory.Create();
        var pipe = new Pipe(new PipeOptions(_memoryPool));
        _reader = pipe.Reader;
        _writer = pipe.Writer;
    }

    [Params(0x0, 0x1, 0x10, 0x100, 0x1_000, 0x10_000, 0x100_000, 0x1_000_000)]
    public int DataLength { get; set; }

    [Benchmark(OperationsPerInvoke = InnerLoopCount)]
    public async Task WriteBeginChunkBytes()
    {
        WriteBeginChunkBytes_Write();

        var flushResult = _writer.FlushAsync();

        var result = await _reader.ReadAsync();
        _reader.AdvanceTo(result.Buffer.End, result.Buffer.End);
        await flushResult;
    }

    private void WriteBeginChunkBytes_Write()
    {
        var writer = new BufferWriter<PipeWriter>(_writer);
        var dataLength = DataLength;
        for (int i = 0; i < InnerLoopCount; i++)
        {
            ChunkWriter.WriteBeginChunkBytes(ref writer, dataLength);
        }

        writer.Commit();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _memoryPool.Dispose();
    }
}

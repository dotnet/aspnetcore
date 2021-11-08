// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class PipeThroughputBenchmark
{
    private const int _writeLength = 57;
    private const int InnerLoopCount = 512;

    private Pipe _pipe;
    private MemoryPool<byte> _memoryPool;

    [IterationSetup]
    public void Setup()
    {
        _memoryPool = PinnedBlockMemoryPoolFactory.Create();
        _pipe = new Pipe(new PipeOptions(_memoryPool));
    }

    [Benchmark(OperationsPerInvoke = InnerLoopCount)]
    public void ParseLiveAspNetTwoTasks()
    {
        var writing = Task.Run(async () =>
        {
            for (int i = 0; i < InnerLoopCount; i++)
            {
                _pipe.Writer.GetMemory(_writeLength);
                _pipe.Writer.Advance(_writeLength);
                await _pipe.Writer.FlushAsync();
            }
        });

        var reading = Task.Run(async () =>
        {
            long remaining = InnerLoopCount * _writeLength;
            while (remaining != 0)
            {
                var result = await _pipe.Reader.ReadAsync();
                remaining -= result.Buffer.Length;
                _pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);
            }
        });

        Task.WaitAll(writing, reading);
    }

    [Benchmark(OperationsPerInvoke = InnerLoopCount)]
    public void ParseLiveAspNetInline()
    {
        for (int i = 0; i < InnerLoopCount; i++)
        {
            _pipe.Writer.GetMemory(_writeLength);
            _pipe.Writer.Advance(_writeLength);
            _pipe.Writer.FlushAsync().GetAwaiter().GetResult();
            var result = _pipe.Reader.ReadAsync().GetAwaiter().GetResult();
            _pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);
        }
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _memoryPool.Dispose();
    }
}

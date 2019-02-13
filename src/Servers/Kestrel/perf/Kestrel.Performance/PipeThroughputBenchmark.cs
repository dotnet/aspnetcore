// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)]
    public class PipeThroughputBenchmark
    {
        private const int InnerLoopCount = 512;

        private PipeReader _reader;
        private PipeWriter _writer;
        private MemoryPool<byte> _memoryPool;

        [GlobalSetup]
        public void Setup()
        {
            _memoryPool = KestrelMemoryPool.Create();

            var chunkLength = Length / Chunks;
            if (chunkLength > _memoryPool.MaxBufferSize)
            {
                // Parallel test will deadlock if too large (waiting for second Task to complete), so N/A that run
                throw new InvalidOperationException();
            }

            if (Length != chunkLength * Chunks)
            {
                // Test will deadlock waiting for data so N/A that run
                throw new InvalidOperationException();
            }

            var pipe = new Pipe(new PipeOptions(_memoryPool));
            _reader = pipe.Reader;
            _writer = pipe.Writer;
        }

        [Params(128, 4096)]
        public int Length { get; set; }

        [Params(1, 2, 4, 16)]
        public int Chunks { get; set; }

        [Benchmark(OperationsPerInvoke = InnerLoopCount)]
        public Task Parse_ParallelAsync()
        {
            // Seperate implementation to ensure tiered compilation can compile while "in-flow"
            return Parse_ParallelAsyncImpl();
        }

        private Task Parse_ParallelAsyncImpl()
        {
            var writing = Task.Run(async () =>
            {
                var chunks = Chunks;
                var chunkLength = Length / chunks;

                for (int i = 0; i < InnerLoopCount; i++)
                {
                    for (var c = 0; c < chunks; c++)
                    {
                        _writer.GetMemory(chunkLength);
                        _writer.Advance(chunkLength);
                    }

                    await _writer.FlushAsync();
                }
            });

            var reading = Task.Run(async () =>
            {
                long remaining = Length * InnerLoopCount;
                while (remaining != 0)
                {
                    var result = await _reader.ReadAsync();
                    remaining -= result.Buffer.Length;
                    _reader.AdvanceTo(result.Buffer.End, result.Buffer.End);
                }
            });

            return Task.WhenAll(writing, reading);
        }

        [Benchmark(OperationsPerInvoke = InnerLoopCount)]
        public Task Parse_SequentialAsync()
        {
            // Seperate implementation to ensure tiered compilation can compile while "in-flow"
            return Parse_SequentialAsyncImpl();
        }

        private async Task Parse_SequentialAsyncImpl()
        {
            var chunks = Chunks;
            var chunkLength = Length / chunks;

            for (int i = 0; i < InnerLoopCount; i++)
            {
                for (var c = 0; c < chunks; c++)
                {
                    _writer.GetMemory(chunkLength);
                    _writer.Advance(chunkLength);
                }

                await _writer.FlushAsync();

                var result = await _reader.ReadAsync();
                _reader.AdvanceTo(result.Buffer.End, result.Buffer.End);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _memoryPool.Dispose();
        }
    }
}

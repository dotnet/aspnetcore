// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    [ParameterizedJobConfig(typeof(CoreConfig))]
    public class PipeThroughputBenchmark
    {
        private const int _writeLenght = 57;
        private const int InnerLoopCount = 512;

        private IPipe _pipe;
        private MemoryPool _memoryPool;

        [IterationSetup]
        public void Setup()
        {
            _memoryPool = new MemoryPool();
            _pipe = new Pipe(new PipeOptions(_memoryPool));
        }

        [Benchmark(OperationsPerInvoke = InnerLoopCount)]
        public void ParseLiveAspNetTwoTasks()
        {
            var writing = Task.Run(async () =>
            {
                for (int i = 0; i < InnerLoopCount; i++)
                {
                    var writableBuffer = _pipe.Writer.Alloc(_writeLenght);
                    writableBuffer.Advance(_writeLenght);
                    await writableBuffer.FlushAsync();
                }
            });

            var reading = Task.Run(async () =>
            {
                long remaining = InnerLoopCount * _writeLenght;
                while (remaining != 0)
                {
                    var result = await _pipe.Reader.ReadAsync();
                    remaining -= result.Buffer.Length;
                    _pipe.Reader.Advance(result.Buffer.End, result.Buffer.End);
                }
            });

            Task.WaitAll(writing, reading);
        }

        [Benchmark(OperationsPerInvoke = InnerLoopCount)]
        public void ParseLiveAspNetInline()
        {
            for (int i = 0; i < InnerLoopCount; i++)
            {
                var writableBuffer = _pipe.Writer.Alloc(_writeLenght);
                writableBuffer.Advance(_writeLenght);
                writableBuffer.FlushAsync().GetAwaiter().GetResult();
                var result = _pipe.Reader.ReadAsync().GetAwaiter().GetResult();
                _pipe.Reader.Advance(result.Buffer.End, result.Buffer.End);
            }
        }
    }
}

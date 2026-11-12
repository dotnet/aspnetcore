// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class BufferWriterWriteNumericBenchmark
{
    private const int Iterations = 1000;

    private NullPipeWriter _pipeWriter;

    [Params(5UL, 421UL, 4096UL, 1_048_576UL, 123_456_789_012UL)]
    public ulong Number { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _pipeWriter = new NullPipeWriter();
    }

    [Benchmark(OperationsPerInvoke = Iterations)]
    public void WriteNumeric()
    {
        var writer = new BufferWriter<PipeWriter>(_pipeWriter);
        var number = Number;
        for (var i = 0; i < Iterations; i++)
        {
            writer.WriteNumeric(number);
        }
        writer.Commit();
    }
}

public class BufferWriterWriteAsciiBenchmark
{
    private const int Iterations = 1000;

    private NullPipeWriter _pipeWriter;
    private string _asciiValue;

    [Params(10, 30, 100)]
    public int StringLength { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _pipeWriter = new NullPipeWriter();
        _asciiValue = new string('a', StringLength);
    }

    [Benchmark(OperationsPerInvoke = Iterations)]
    public void WriteAscii()
    {
        var writer = new BufferWriter<PipeWriter>(_pipeWriter);
        var value = _asciiValue;
        for (var i = 0; i < Iterations; i++)
        {
            writer.WriteAscii(value);
        }
        writer.Commit();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class MessageParserBenchmark
{
    private byte[] _binaryInput;
    private byte[] _textInput;

    [Params(32, 64)]
    public int ChunkSize { get; set; }

    [Params(64, 128)]
    public int MessageLength { get; set; }

    [IterationSetup]
    public void Setup()
    {
        var buffer = new byte[MessageLength];
        Random.Shared.NextBytes(buffer);
        var writer = MemoryBufferWriter.Get();
        try
        {
            BinaryMessageFormatter.WriteLengthPrefix(buffer.Length, writer);
            writer.Write(buffer);
            _binaryInput = writer.ToArray();
        }
        finally
        {
            MemoryBufferWriter.Return(writer);
        }

        buffer = new byte[MessageLength];
        Random.Shared.NextBytes(buffer);
        writer = MemoryBufferWriter.Get();
        try
        {
            writer.Write(buffer);
            TextMessageFormatter.WriteRecordSeparator(writer);

            _textInput = writer.ToArray();
        }
        finally
        {
            MemoryBufferWriter.Return(writer);
        }
    }

    [Benchmark]
    public void SingleBinaryMessage()
    {
        var data = new ReadOnlySequence<byte>(_binaryInput);
        if (!BinaryMessageParser.TryParseMessage(ref data, out _))
        {
            throw new InvalidOperationException("Failed to parse");
        }
    }

    [Benchmark]
    public void SingleTextMessage()
    {
        var data = new ReadOnlySequence<byte>(_textInput);
        if (!TextMessageParser.TryParseMessage(ref data, out _))
        {
            throw new InvalidOperationException("Failed to parse");
        }
    }
}

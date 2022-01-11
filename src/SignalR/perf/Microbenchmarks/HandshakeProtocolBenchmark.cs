// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class HandshakeProtocolBenchmark
{
    ReadOnlySequence<byte> _requestMessage1;
    ReadOnlySequence<byte> _requestMessage2;
    ReadOnlySequence<byte> _requestMessage3;
    ReadOnlySequence<byte> _requestMessage4;
    ReadOnlySequence<byte> _requestMessage5;

    ReadOnlySequence<byte> _responseMessage1;
    ReadOnlySequence<byte> _responseMessage2;
    ReadOnlySequence<byte> _responseMessage3;
    ReadOnlySequence<byte> _responseMessage4;
    ReadOnlySequence<byte> _responseMessage5;
    ReadOnlySequence<byte> _responseMessage6;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _requestMessage1 = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("{\"protocol\":\"dummy\",\"version\":1}\u001e"));
        _requestMessage2 = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("{\"protocol\":\"\",\"version\":10}\u001e"));
        _requestMessage3 = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("{\"protocol\":\"\",\"version\":10,\"unknown\":null}\u001e"));
        _requestMessage4 = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("42"));
        _requestMessage5 = ReadOnlySequenceFactory.CreateSegments(Encoding.UTF8.GetBytes("{\"protocol\":\"dummy\",\"ver"), Encoding.UTF8.GetBytes("sion\":1}\u001e"));

        _responseMessage1 = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("{\"error\":\"dummy\"}\u001e"));
        _responseMessage2 = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("{\"error\":\"\"}\u001e"));
        _responseMessage3 = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("{}\u001e"));
        _responseMessage4 = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("{\"unknown\":null}\u001e"));
        _responseMessage5 = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("{\"error\":\"\",\"minorVersion\":34}\u001e"));
        _responseMessage6 = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("{\"error\":\"flump flump flump\",\"minorVersion\":112}\u001e"));
    }

    [Benchmark]
    public void HandShakeWriteResponseEmpty_MemoryBufferWriter()
    {
        var writer = MemoryBufferWriter.Get();
        try
        {
            HandshakeProtocol.WriteResponseMessage(HandshakeResponseMessage.Empty, writer);
        }
        finally
        {
            MemoryBufferWriter.Return(writer);
        }
    }

    [Benchmark]
    public void HandShakeWriteResponse_MemoryBufferWriter()
    {
        ReadOnlyMemory<byte> result;
        var memoryBufferWriter = MemoryBufferWriter.Get();
        try
        {
            HandshakeProtocol.WriteResponseMessage(HandshakeResponseMessage.Empty, memoryBufferWriter);
            result = memoryBufferWriter.ToArray();
        }
        finally
        {
            MemoryBufferWriter.Return(memoryBufferWriter);
        }
    }

    [Benchmark]
    public void HandShakeWriteRequest_MemoryBufferWriter()
    {
        var memoryBufferWriter = MemoryBufferWriter.Get();
        try
        {
            HandshakeProtocol.WriteRequestMessage(new HandshakeRequestMessage("json", 1), memoryBufferWriter);
        }
        finally
        {
            MemoryBufferWriter.Return(memoryBufferWriter);
        }
    }

    [Benchmark]
    public void ParsingHandshakeRequestMessage_ValidMessage1()
    {
        var message = _requestMessage1;
        if (!HandshakeProtocol.TryParseRequestMessage(ref message, out var deserializedMessage))
        {
            throw new Exception();
        }
    }

    [Benchmark]
    public void ParsingHandshakeRequestMessage_ValidMessage2()
    {
        var message = _requestMessage2;
        if (!HandshakeProtocol.TryParseRequestMessage(ref message, out var deserializedMessage))
        {
            throw new Exception();
        }
    }

    [Benchmark]
    public void ParsingHandshakeRequestMessage_ValidMessage3()
    {
        var message = _requestMessage3;
        if (!HandshakeProtocol.TryParseRequestMessage(ref message, out var deserializedMessage))
        {
            throw new Exception();
        }
    }

    [Benchmark]
    public void ParsingHandshakeRequestMessage_NotComplete1()
    {
        var message = _requestMessage4;
        if (HandshakeProtocol.TryParseRequestMessage(ref message, out var deserializedMessage))
        {
            throw new Exception();
        }
    }

    [Benchmark]
    public void ParsingHandshakeRequestMessage_ValidMessageSegments()
    {
        var message = _requestMessage5;
        if (!HandshakeProtocol.TryParseRequestMessage(ref message, out var deserializedMessage))
        {
            throw new Exception();
        }
    }

    [Benchmark]
    public void ParsingHandshakeResponseMessage_ValidMessages1()
    {
        var message = _responseMessage1;
        if (!HandshakeProtocol.TryParseResponseMessage(ref message, out var deserializedMessage))
        {
            throw new Exception();
        }
    }

    [Benchmark]
    public void ParsingHandshakeResponseMessage_ValidMessages2()
    {
        var message = _responseMessage2;
        if (!HandshakeProtocol.TryParseResponseMessage(ref message, out var deserializedMessage))
        {
            throw new Exception();
        }
    }

    [Benchmark]
    public void ParsingHandshakeResponseMessage_ValidMessages3()
    {
        var message = _responseMessage3;
        if (!HandshakeProtocol.TryParseResponseMessage(ref message, out var deserializedMessage))
        {
            throw new Exception();
        }
    }

    [Benchmark]
    public void ParsingHandshakeResponseMessage_ValidMessages4()
    {
        var message = _responseMessage4;
        if (!HandshakeProtocol.TryParseResponseMessage(ref message, out var deserializedMessage))
        {
            throw new Exception();
        }
    }

    [Benchmark]
    public void ParsingHandshakeResponseMessage_GivesMinorVersion1()
    {
        var message = _responseMessage5;
        if (!HandshakeProtocol.TryParseResponseMessage(ref message, out var deserializedMessage))
        {
            throw new Exception();
        }
    }

    [Benchmark]
    public void ParsingHandshakeResponseMessage_GivesMinorVersion2()
    {
        var message = _responseMessage6;
        if (!HandshakeProtocol.TryParseResponseMessage(ref message, out var deserializedMessage))
        {
            throw new Exception();
        }
    }
}

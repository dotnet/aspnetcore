// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    public class HandshakeProtocolBenchmark
    {
        ReadOnlySequence<byte> _requestMessage1;
        ReadOnlySequence<byte> _requestMessage2;
        ReadOnlySequence<byte> _requestMessage3;
        ReadOnlySequence<byte> _requestMessage4;

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
                HandshakeProtocol.WriteResponseMessage(new HandshakeResponseMessage(1), memoryBufferWriter);
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
            => HandshakeProtocol.TryParseRequestMessage(ref _requestMessage1, out var deserializedMessage);

        [Benchmark]
        public void ParsingHandshakeRequestMessage_ValidMessage2()
            => HandshakeProtocol.TryParseRequestMessage(ref _requestMessage2, out var deserializedMessage);

        [Benchmark]
        public void ParsingHandshakeRequestMessage_ValidMessage3()
            => HandshakeProtocol.TryParseRequestMessage(ref _requestMessage3, out var deserializedMessage);

        [Benchmark]
        public void ParsingHandshakeRequestMessage_NotComplete1()
            => HandshakeProtocol.TryParseRequestMessage(ref _requestMessage4, out _);

        [Benchmark]
        public void ParsingHandshakeResponseMessage_ValidMessages1()
            => HandshakeProtocol.TryParseResponseMessage(ref _responseMessage1, out var response);

        [Benchmark]
        public void ParsingHandshakeResponseMessage_ValidMessages2()
            => HandshakeProtocol.TryParseResponseMessage(ref _responseMessage2, out var response);

        [Benchmark]
        public void ParsingHandshakeResponseMessage_ValidMessages3()
            => HandshakeProtocol.TryParseResponseMessage(ref _responseMessage3, out var response);

        [Benchmark]
        public void ParsingHandshakeResponseMessage_ValidMessages4() 
            => HandshakeProtocol.TryParseResponseMessage(ref _responseMessage4, out var response);

        [Benchmark]
        public void ParsingHandshakeResponseMessage_GivesMinorVersion1()
            => HandshakeProtocol.TryParseResponseMessage(ref _responseMessage5, out var response);

        [Benchmark]
        public void ParsingHandshakeResponseMessage_GivesMinorVersion2()
            => HandshakeProtocol.TryParseResponseMessage(ref _responseMessage6, out var response);
    }
}
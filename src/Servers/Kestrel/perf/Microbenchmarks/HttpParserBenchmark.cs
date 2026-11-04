// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class HttpParserBenchmark : IHttpRequestLineHandler, IHttpHeadersHandler
{
    private readonly HttpParser<Adapter> _parser = new HttpParser<Adapter>();

    private ReadOnlySequence<byte> _buffer;
    private ReadOnlySequence<byte> _multispanHeader;

    [GlobalSetup]
    public void Setup()
    {
        var segment = new BufferSegment();
        var split = RequestParsingData.UnicodeRequest.Length / 2;
        segment.SetOwnedMemory(RequestParsingData.UnicodeRequest.AsSpan(0, split).ToArray());
        segment.End = split;
        var next = new BufferSegment();
        next.SetOwnedMemory(RequestParsingData.UnicodeRequest.AsSpan(split).ToArray());
        next.End = split;
        segment.SetNext(next);
        _multispanHeader = new ReadOnlySequence<byte>(segment, 0, next, next.Memory.Length);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = RequestParsingData.InnerLoopCount)]
    public void PlaintextTechEmpower()
    {
        for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
        {
            InsertData(RequestParsingData.PlaintextTechEmpowerRequest);
            ParseData();
        }
    }

    [Benchmark(OperationsPerInvoke = RequestParsingData.InnerLoopCount)]
    public void JsonTechEmpower()
    {
        for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
        {
            InsertData(RequestParsingData.JsonTechEmpowerRequest);
            ParseData();
        }
    }

    [Benchmark(OperationsPerInvoke = RequestParsingData.InnerLoopCount)]
    public void LiveAspNet()
    {
        for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
        {
            InsertData(RequestParsingData.LiveaspnetRequest);
            ParseData();
        }
    }

    [Benchmark(OperationsPerInvoke = RequestParsingData.InnerLoopCount)]
    public void Unicode()
    {
        for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
        {
            InsertData(RequestParsingData.UnicodeRequest);
            ParseData();
        }
    }

    [Benchmark(OperationsPerInvoke = RequestParsingData.InnerLoopCount)]
    public void MultispanUnicodeHeader()
    {
        for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
        {
            _buffer = _multispanHeader;
            ParseData();
        }
    }

    private void InsertData(byte[] data)
    {
        _buffer = new ReadOnlySequence<byte>(data);
    }

    private void ParseData()
    {
        var reader = new SequenceReader<byte>(_buffer);
        if (!_parser.ParseRequestLine(new Adapter(this), ref reader))
        {
            ErrorUtilities.ThrowInvalidRequestHeaders();
        }

        if (!_parser.ParseHeaders(new Adapter(this), ref reader))
        {
            ErrorUtilities.ThrowInvalidRequestHeaders();
        }
    }

    public void OnStartLine(HttpVersionAndMethod versionAndMethod, TargetOffsetPathLength targetPath, Span<byte> startLine)
    {
    }

    public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
    }

    public void OnHeadersComplete(bool endStream)
    {
    }

    public void OnStaticIndexedHeader(int index)
    {
        throw new NotImplementedException();
    }

    public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
    {
        throw new NotImplementedException();
    }

    private struct Adapter : IHttpRequestLineHandler, IHttpHeadersHandler
    {
        public HttpParserBenchmark RequestHandler;

        public Adapter(HttpParserBenchmark requestHandler)
        {
            RequestHandler = requestHandler;
        }

        public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
            => RequestHandler.OnHeader(name, value);

        public void OnHeadersComplete(bool endStream)
            => RequestHandler.OnHeadersComplete(endStream);

        public void OnStartLine(HttpVersionAndMethod versionAndMethod, TargetOffsetPathLength targetPath, Span<byte> startLine)
            => RequestHandler.OnStartLine(versionAndMethod, targetPath, startLine);

        public void OnStaticIndexedHeader(int index)
        {
            throw new NotImplementedException();
        }

        public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
        {
            throw new NotImplementedException();
        }
    }
}

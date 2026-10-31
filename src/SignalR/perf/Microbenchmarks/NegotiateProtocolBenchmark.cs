// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class NegotiateProtocolBenchmark
{
    private NegotiationResponse _negotiateResponse;
    private Stream _stream;

    private byte[] _responseData1;
    private byte[] _responseData2;
    private byte[] _responseData3;
    private byte[] _responseData4;
    private byte[] _responseData5;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _negotiateResponse = new NegotiationResponse
        {
            ConnectionId = "d100338e-8c01-4281-92c2-9a967fdeebcb",
            AvailableTransports = new List<AvailableTransport>
                {
                    new AvailableTransport
                    {
                        Transport = "WebSockets",
                        TransferFormats = new List<string>
                        {
                            "Text",
                            "Binary"
                        }
                    }
                }
        };
        _stream = Stream.Null;

        _responseData1 = Encoding.UTF8.GetBytes("{\"connectionId\":\"123\",\"availableTransports\":[]}");
        _responseData2 = Encoding.UTF8.GetBytes("{\"url\": \"http://foo.com/chat\"}");
        _responseData3 = Encoding.UTF8.GetBytes("{\"url\": \"http://foo.com/chat\", \"accessToken\": \"token\"}");
        _responseData4 = Encoding.UTF8.GetBytes("{\"connectionId\":\"123\",\"availableTransports\":[{\"transport\":\"test\",\"transferFormats\":[]}]}");

        var writer = new MemoryBufferWriter();
        NegotiateProtocol.WriteResponse(_negotiateResponse, writer);
        _responseData5 = writer.ToArray();
    }

    [Benchmark]
    public Task WriteResponse_MemoryBufferWriter()
    {
        var writer = new MemoryBufferWriter();
        try
        {
            NegotiateProtocol.WriteResponse(_negotiateResponse, writer);
            return writer.CopyToAsync(_stream);
        }
        finally
        {
            writer.Reset();
        }
    }

    [Benchmark]
    public void ParsingNegotiateResponseMessageSuccessForValid1()
        => NegotiateProtocol.ParseResponse(_responseData1);

    [Benchmark]
    public void ParsingNegotiateResponseMessageSuccessForValid2()
        => NegotiateProtocol.ParseResponse(_responseData2);

    [Benchmark]
    public void ParsingNegotiateResponseMessageSuccessForValid3()
        => NegotiateProtocol.ParseResponse(_responseData3);

    [Benchmark]
    public void ParsingNegotiateResponseMessageSuccessForValid4()
        => NegotiateProtocol.ParseResponse(_responseData4);

    [Benchmark]
    public void ParsingNegotiateResponseMessageSuccessForValid5()
        => NegotiateProtocol.ParseResponse(_responseData5);
}

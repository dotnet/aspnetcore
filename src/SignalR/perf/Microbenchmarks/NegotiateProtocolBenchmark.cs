// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
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
            => NegotiateProtocol.ParseResponse(new MemoryStream(_responseData1));

        [Benchmark]
        public void ParsingNegotiateResponseMessageSuccessForValid2()
            => NegotiateProtocol.ParseResponse(new MemoryStream(_responseData2));

        [Benchmark]
        public void ParsingNegotiateResponseMessageSuccessForValid3()
            => NegotiateProtocol.ParseResponse(new MemoryStream(_responseData3));

        [Benchmark]
        public void ParsingNegotiateResponseMessageSuccessForValid4()
            => NegotiateProtocol.ParseResponse(new MemoryStream(_responseData4));

        [Benchmark]
        public void ParsingNegotiateResponseMessageSuccessForValid5()
            => NegotiateProtocol.ParseResponse(new MemoryStream(_responseData5));
    }
}
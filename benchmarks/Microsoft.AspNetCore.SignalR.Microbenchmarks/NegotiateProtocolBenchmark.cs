// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
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
    }
}

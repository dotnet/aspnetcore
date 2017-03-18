// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using BenchmarkDotNet.Attributes;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    [Config(typeof(CoreConfig))]
    public class ResponseHeadersWritingBenchmark
    {
        private static readonly byte[] _helloWorldPayload = Encoding.ASCII.GetBytes("Hello, World!");

        private readonly TestFrame<object> _frame;

        public ResponseHeadersWritingBenchmark()
        {
            _frame = MakeFrame();
        }

        public enum BenchmarkTypes
        {
            TechEmpowerPlaintext,
            PlaintextChunked,
            PlaintextWithCookie,
            PlaintextChunkedWithCookie,
            LiveAspNet
        }

        [Params(
            BenchmarkTypes.TechEmpowerPlaintext,
            BenchmarkTypes.PlaintextChunked,
            BenchmarkTypes.PlaintextWithCookie,
            BenchmarkTypes.PlaintextChunkedWithCookie,
            BenchmarkTypes.LiveAspNet
        )]
        public BenchmarkTypes Type { get; set; }

        [Benchmark]
        public async Task Output()
        {
            _frame.Reset();
            _frame.StatusCode = 200;

            Task writeTask = Task.CompletedTask;
            switch (Type)
            {
                case BenchmarkTypes.TechEmpowerPlaintext:
                    writeTask = TechEmpowerPlaintext();
                    break;
                case BenchmarkTypes.PlaintextChunked:
                    writeTask = PlaintextChunked();
                    break;
                case BenchmarkTypes.PlaintextWithCookie:
                    writeTask = PlaintextWithCookie();
                    break;
                case BenchmarkTypes.PlaintextChunkedWithCookie:
                    writeTask = PlaintextChunkedWithCookie();
                    break;
                case BenchmarkTypes.LiveAspNet:
                    writeTask = LiveAspNet();
                    break;
            }

            await writeTask;
            await _frame.ProduceEndAsync();
        }

        private Task TechEmpowerPlaintext()
        {
            var responseHeaders = _frame.ResponseHeaders;
            responseHeaders["Content-Type"] = "text/plain";
            responseHeaders.ContentLength = _helloWorldPayload.Length;
            return _frame.WriteAsync(new ArraySegment<byte>(_helloWorldPayload), default(CancellationToken));
        }

        private Task PlaintextChunked()
        {
            var responseHeaders = _frame.ResponseHeaders;
            responseHeaders["Content-Type"] = "text/plain";
            return _frame.WriteAsync(new ArraySegment<byte>(_helloWorldPayload), default(CancellationToken));
        }

        private Task LiveAspNet()
        {
            var responseHeaders = _frame.ResponseHeaders;
            responseHeaders["Content-Encoding"] = "gzip";
            responseHeaders["Content-Type"] = "text/html; charset=utf-8";
            responseHeaders["Strict-Transport-Security"] = "max-age=31536000; includeSubdomains";
            responseHeaders["Vary"] = "Accept-Encoding";
            responseHeaders["X-Powered-By"] = "ASP.NET";
            return _frame.WriteAsync(new ArraySegment<byte>(_helloWorldPayload), default(CancellationToken));
        }

        private Task PlaintextWithCookie()
        {
            var responseHeaders = _frame.ResponseHeaders;
            responseHeaders["Content-Type"] = "text/plain";
            responseHeaders["Set-Cookie"] = "prov=20629ccd-8b0f-e8ef-2935-cd26609fc0bc; __qca=P0-1591065732-1479167353442; _ga=GA1.2.1298898376.1479167354; _gat=1; sgt=id=9519gfde_3347_4762_8762_df51458c8ec2; acct=t=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric&s=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric";
            responseHeaders.ContentLength = _helloWorldPayload.Length;
            return _frame.WriteAsync(new ArraySegment<byte>(_helloWorldPayload), default(CancellationToken));
        }

        private Task PlaintextChunkedWithCookie()
        {
            var responseHeaders = _frame.ResponseHeaders;
            responseHeaders["Content-Type"] = "text/plain";
            responseHeaders["Set-Cookie"] = "prov=20629ccd-8b0f-e8ef-2935-cd26609fc0bc; __qca=P0-1591065732-1479167353442; _ga=GA1.2.1298898376.1479167354; _gat=1; sgt=id=9519gfde_3347_4762_8762_df51458c8ec2; acct=t=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric&s=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric";
            return _frame.WriteAsync(new ArraySegment<byte>(_helloWorldPayload), default(CancellationToken));
        }

        private TestFrame<object> MakeFrame()
        {
            var socketInput = new PipeFactory().Create();

            var serviceContext = new ServiceContext
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerOptions = new KestrelServerOptions(),
                Log = Mock.Of<IKestrelTrace>()
            };
            var listenerContext = new ListenerContext(serviceContext)
            {
                ListenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 5000))
            };
            var connectionContext = new ConnectionContext(listenerContext)
            {
                Input = socketInput,
                Output = new MockSocketOutput(),
                ConnectionControl = Mock.Of<IConnectionControl>()
            };
            connectionContext.ListenerContext.ServiceContext.HttpParserFactory = f => new Internal.Http.KestrelHttpParser(log: null);

            var frame = new TestFrame<object>(application: null, context: connectionContext);
            frame.Reset();
            frame.InitializeHeaders();

            return frame;
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using RequestLineStatus = Microsoft.AspNetCore.Server.Kestrel.Internal.Http.Frame.RequestLineStatus;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    [Config(typeof(CoreConfig))]
    public class RequestParsing
    {
        private const int InnerLoopCount = 512;
        private const int Pipelining = 16;

        private const string plaintextRequest = "GET /plaintext HTTP/1.1\r\nHost: www.example.com\r\n\r\n";

        private const string liveaspnetRequest = "GET https://live.asp.net/ HTTP/1.1\r\n" + 
            "Host: live.asp.net\r\n" + 
            "Connection: keep-alive\r\n" + 
            "Upgrade-Insecure-Requests: 1\r\n" + 
            "User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.99 Safari/537.36\r\n" + 
            "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8\r\n" + 
            "DNT: 1\r\n" + 
            "Accept-Encoding: gzip, deflate, sdch, br\r\n" + 
            "Accept-Language: en-US,en;q=0.8\r\n" +
            "Cookie: __unam=7a67379-1s65dc575c4-6d778abe-1; omniID=9519gfde_3347_4762_8762_df51458c8ec2\r\n\r\n";

        private const string unicodeRequest =
            "GET http://stackoverflow.com/questions/40148683/why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric HTTP/1.1\r\n" +
            "Accept: text/html, application/xhtml+xml, image/jxr, */*\r\n" +
            "Accept-Language: en-US,en-GB;q=0.7,en;q=0.3\r\n" +
            "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36 Edge/15.14965\r\n" +
            "Accept-Encoding: gzip, deflate\r\n" +
            "Host: stackoverflow.com\r\n" +
            "Connection: Keep-Alive\r\n" +
            "Cache-Control: max-age=0\r\n" +
            "Upgrade-Insecure-Requests: 1\r\n" +
            "DNT: 1\r\n" +
            "Referer: http://stackoverflow.com/?tab=month\r\n" +
            "Pragma: no-cache\r\n" +
            "Cookie: prov=20629ccd-8b0f-e8ef-2935-cd26609fc0bc; __qca=P0-1591065732-1479167353442; _ga=GA1.2.1298898376.1479167354; _gat=1; sgt=id=9519gfde_3347_4762_8762_df51458c8ec2; acct=t=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric&s=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric\r\n\r\n";

        private static readonly byte[] _plaintextPipelinedRequests = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat(plaintextRequest, Pipelining)));
        private static readonly byte[] _plaintextRequest  = Encoding.ASCII.GetBytes(plaintextRequest);

        private static readonly byte[] _liveaspnentPipelinedRequests = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat(liveaspnetRequest, Pipelining)));
        private static readonly byte[] _liveaspnentRequest = Encoding.ASCII.GetBytes(liveaspnetRequest);

        private static readonly byte[] _unicodePipelinedRequests = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat(unicodeRequest, Pipelining)));
        private static readonly byte[] _unicodeRequest = Encoding.ASCII.GetBytes(unicodeRequest);

        private KestrelTrace Trace;
        private LoggingThreadPool ThreadPool;
        private MemoryPool MemoryPool;
        private SocketInput SocketInput;
        private Frame<object> Frame;

        [Benchmark(Baseline = true, OperationsPerInvoke = InnerLoopCount)]
        public void ParsePlaintext()
        {
            for (var i = 0; i < InnerLoopCount; i++)
            {
                InsertData(_plaintextRequest);

                ParseData();
            }
        }

        [Benchmark(OperationsPerInvoke = InnerLoopCount * Pipelining)]
        public void ParsePipelinedPlaintext()
        {
            for (var i = 0; i < InnerLoopCount; i++)
            {
                InsertData(_plaintextPipelinedRequests);

                ParseData();
            }
        }

        [Benchmark(OperationsPerInvoke = InnerLoopCount)]
        public void ParseLiveAspNet()
        {
            for (var i = 0; i < InnerLoopCount; i++)
            {
                InsertData(_liveaspnentRequest);

                ParseData();
            }
        }

        [Benchmark(OperationsPerInvoke = InnerLoopCount * Pipelining)]
        public void ParsePipelinedLiveAspNet()
        {
            for (var i = 0; i < InnerLoopCount; i++)
            {
                InsertData(_liveaspnentPipelinedRequests);

                ParseData();
            }
        }

        [Benchmark(OperationsPerInvoke = InnerLoopCount)]
        public void ParseUnicode()
        {
            for (var i = 0; i < InnerLoopCount; i++)
            {
                InsertData(_unicodeRequest);

                ParseData();
            }
        }

        [Benchmark(OperationsPerInvoke = InnerLoopCount * Pipelining)]
        public void ParseUnicodePipelined()
        {
            for (var i = 0; i < InnerLoopCount; i++)
            {
                InsertData(_unicodePipelinedRequests);

                ParseData();
            }
        }

        private void InsertData(byte[] dataBytes)
        {
            SocketInput.IncomingData(dataBytes, 0, dataBytes.Length);
        }

        private void ParseData()
        {
            while (SocketInput.GetAwaiter().IsCompleted)
            {
                Frame.Reset();

                if (Frame.TakeStartLine(SocketInput) != RequestLineStatus.Done)
                {
                    ThrowInvalidStartLine();
                }

                Frame.InitializeHeaders();

                if (!Frame.TakeMessageHeaders(SocketInput, (FrameRequestHeaders) Frame.RequestHeaders))
                {
                    ThrowInvalidMessageHeaders();
                }
            }
        }

        private void ThrowInvalidStartLine()
        {
            throw new InvalidOperationException("Invalid StartLine");
        }

        private void ThrowInvalidMessageHeaders()
        {
            throw new InvalidOperationException("Invalid MessageHeaders");
        }

        [Setup]
        public void Setup()
        {
            Trace = new KestrelTrace(new TestKestrelTrace());
            ThreadPool = new LoggingThreadPool(Trace);
            MemoryPool = new MemoryPool();
            SocketInput = new SocketInput(MemoryPool, ThreadPool);

            var connectionContext = new MockConnection(new KestrelServerOptions());
            connectionContext.Input = SocketInput;

            Frame = new Frame<object>(application: null, context: connectionContext);
        }

        [Cleanup]
        public void Cleanup()
        {
            SocketInput.IncomingFin();
            SocketInput.Dispose();
            MemoryPool.Dispose();
        }
    }
}

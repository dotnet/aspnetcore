// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class Http2ConnectionHeadersBenchmark : Http2ConnectionBenchmarkBase
    {
        [Params(1, 4, 32)]
        public int HeadersCount { get; set; }

        [Params(true, false)]
        public bool HeadersChange { get; set; }

        private int _headerIndex;
        private string[] _headerNames;

        [GlobalSetup]
        public override void GlobalSetup()
        {
            base.GlobalSetup();

            _headerNames = new string[HeadersCount * (HeadersChange ? 1000 : 1)];
            for (var i = 0; i < _headerNames.Length; i++)
            {
                _headerNames[i] = "CustomHeader" + i;
            }
        }

        protected override Task ProcessRequest(HttpContext httpContext)
        {
            for (var i = 0; i < HeadersCount; i++)
            {
                var headerName = _headerNames[_headerIndex % HeadersCount];
                httpContext.Response.Headers[headerName] = "The quick brown fox jumps over the lazy dog.";
                if (HeadersChange)
                {
                    _headerIndex++;
                }
            }
            
            return Task.CompletedTask;
        }
    }
}

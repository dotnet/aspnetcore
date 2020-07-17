// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class Http2ConnectionBenchmark : Http2ConnectionBenchmarkBase
    {
        [Params(0, 128, 1024)]
        public int ResponseDataLength { get; set; }

        private string _responseData;

        [GlobalSetup]
        public override void GlobalSetup()
        {
            base.GlobalSetup();
            _responseData = new string('!', ResponseDataLength);
        }

        protected override Task ProcessRequest(HttpContext httpContext)
        {
            return ResponseDataLength == 0 ? Task.CompletedTask : httpContext.Response.WriteAsync(_responseData);
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCompression.Benchmarks
{
    public class ResponseCompressionProviderBenchmark
    {
        [GlobalSetup]
        public void GlobalSetup()
        {
            var services = new ServiceCollection()
                .AddOptions()
                .AddResponseCompression()
                .BuildServiceProvider();

            var options = new ResponseCompressionOptions();

            Provider = new ResponseCompressionProvider(services, Options.Create(options));
        }

        [ParamsSource(nameof(EncodingStrings))]
        public string AcceptEncoding { get; set; }

        public static IEnumerable<string> EncodingStrings()
        {
            return new[]
            {
                "gzip;q=0.8, compress;q=0.6, br;q=0.4",
                "gzip, compress, br",
                "br, compress, gzip",
                "gzip, compress",
                "identity",
                "*"
            };
        }

        public ResponseCompressionProvider Provider { get; set; }

        [Benchmark]
        public ICompressionProvider GetCompressionProvider()
        {
            var context = new DefaultHttpContext();

            context.Request.Headers[HeaderNames.AcceptEncoding] = AcceptEncoding;

            return Provider.GetCompressionProvider(context);
        }
    }
}
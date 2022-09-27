// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ResponseCompression.Benchmarks;

public class ResponseCompressionProviderBenchmark
{
    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection()
            .AddOptions()
            .AddResponseCompression()
            .AddLogging()
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

        context.Request.Headers.AcceptEncoding = AcceptEncoding;

        return Provider.GetCompressionProvider(context);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ResponseCompression.Benchmarks;

[MemoryDiagnoser]
public class ShouldCompressResponseBenchmark
{
    private ResponseCompressionProvider _provider;
    private HttpContext _context;

    [Params("application/json; charset=utf-8", "text/html", "image/png")]
    public string ContentType { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection()
            .AddOptions()
            .AddResponseCompression()
            .AddLogging()
            .BuildServiceProvider();

        _provider = new ResponseCompressionProvider(services, Options.Create(new ResponseCompressionOptions()));
        _context = new DefaultHttpContext();
        _context.Response.ContentType = ContentType;
    }

    [Benchmark]
    public bool ShouldCompressResponse() => _provider.ShouldCompressResponse(_context);
}

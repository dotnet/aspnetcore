// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

[MemoryDiagnoser]
public class HeaderValueToStringBenchmarks
{
    private readonly CacheControlHeaderValue _publicMaxAge = new() { Public = true, MaxAge = TimeSpan.FromDays(365) };
    private readonly CacheControlHeaderValue _noCache = new() { NoCache = true, NoStore = true, MustRevalidate = true };
    private readonly CacheControlHeaderValue _privateMaxAge = new() { Private = true, MaxAge = TimeSpan.FromSeconds(60), SharedMaxAge = TimeSpan.FromSeconds(120) };

    private readonly MediaTypeHeaderValue _mediaType = new("text/html") { Charset = "utf-8" };
    private readonly ContentDispositionHeaderValue _contentDisposition = CreateContentDisposition();
    private readonly ContentRangeHeaderValue _contentRange = new(0, 1023, 1048576) { Unit = "bytes" };

    private static ContentDispositionHeaderValue CreateContentDisposition()
    {
        var value = new ContentDispositionHeaderValue("attachment");
        value.SetHttpFileName("report-2024.pdf");
        return value;
    }

    [Benchmark]
    public string MediaType() => _mediaType.ToString();

    [Benchmark]
    public string ContentDisposition() => _contentDisposition.ToString();

    [Benchmark]
    public string ContentRange() => _contentRange.ToString();

    [Benchmark]
    public string PublicMaxAge() => _publicMaxAge.ToString();

    [Benchmark]
    public string NoCache() => _noCache.ToString();

    [Benchmark]
    public string PrivateMaxAge() => _privateMaxAge.ToString();
}

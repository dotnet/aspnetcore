// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

public class HeaderParsingBenchmarks
{
    private readonly string[] _accept = ["text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8"];
    private readonly string[] _acceptEncoding = ["gzip, deflate, br, zstd"];
    private readonly string[] _acceptLanguage = ["en-US,en;q=0.9,fr-FR;q=0.8,fr;q=0.7"];

    [Benchmark]
    public int ParseAccept() => MediaTypeHeaderValue.ParseList(_accept).Count;

    [Benchmark]
    public int ParseAcceptEncoding() => StringWithQualityHeaderValue.ParseList(_acceptEncoding).Count;

    [Benchmark]
    public int ParseAcceptLanguage() => StringWithQualityHeaderValue.ParseList(_acceptLanguage).Count;
}

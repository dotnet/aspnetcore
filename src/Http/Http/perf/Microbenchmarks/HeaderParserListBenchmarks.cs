// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

public class HeaderParserListBenchmarks
{
    private string[] _entityTagValues = null!;
    private string[] _mediaTypeMissingSlashValues = null!;
    private string[] _mediaTypeMissingSubtypeValues = null!;
    private string[] _setCookieValues = null!;
    private string[] _stringWithQualityValues = null!;

    [Params(16, 256, 4096, 32768)]
    public int Length { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _entityTagValues = [new string(',', Length - 1) + "@"];
        _mediaTypeMissingSlashValues = [new string('a', Length)];
        _mediaTypeMissingSubtypeValues = [new string('a', Length - 1) + "/"];
        _setCookieValues = [new string('a', Length)];
        _stringWithQualityValues = [new string('a', Length - 1) + ";"];
    }

    [Benchmark]
    public int EntityTagSeparators()
    {
        return EntityTagHeaderValue.ParseList(_entityTagValues).Count;
    }

    [Benchmark]
    public int MediaTypeMissingSlash()
    {
        return MediaTypeHeaderValue.ParseList(_mediaTypeMissingSlashValues).Count;
    }

    [Benchmark]
    public int MediaTypeMissingSubtype()
    {
        return MediaTypeHeaderValue.ParseList(_mediaTypeMissingSubtypeValues).Count;
    }

    [Benchmark]
    public int SetCookieMissingEquals()
    {
        return SetCookieHeaderValue.ParseList(_setCookieValues).Count;
    }

    [Benchmark]
    public int StringWithQualityInvalidSuffix()
    {
        return StringWithQualityHeaderValue.ParseList(_stringWithQualityValues).Count;
    }
}

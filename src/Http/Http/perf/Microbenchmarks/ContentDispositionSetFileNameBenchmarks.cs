// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

[MemoryDiagnoser]
public class ContentDispositionSetFileNameBenchmarks
{
    [Benchmark]
    public string AsciiFileName()
    {
        var value = new ContentDispositionHeaderValue("attachment");
        value.SetHttpFileName("report-2024.pdf");
        return value.ToString();
    }

    [Benchmark]
    public string FileNameWithSpaces()
    {
        var value = new ContentDispositionHeaderValue("attachment");
        value.SetHttpFileName("Quarterly Report 2024.pdf");
        return value.ToString();
    }
}

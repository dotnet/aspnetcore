// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

public class ContentDispositionHeaderValueBenchmarks
{
    private readonly ContentDispositionHeaderValue _contentDisposition = new ContentDispositionHeaderValue("inline");

    [Benchmark]
    public void FileNameStarEncoding() => _contentDisposition.FileNameStar = "My TypicalFilename 2024 04 09 08:00:00.dat";

    [Benchmark]
    public void FileNameStarNoEncoding() => _contentDisposition.FileNameStar = "My_TypicalFilename_2024_04_09-08_00_00.dat";

}

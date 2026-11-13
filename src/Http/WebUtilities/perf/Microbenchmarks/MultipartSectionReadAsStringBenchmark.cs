// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.WebUtilities.Microbenchmarks;

public class MultipartSectionReadAsStringBenchmark
{
    private const string Boundary = "9051914041544843365972754266";
    private MemoryStream _stream;

    [Params(1, 5)]
    public int FieldCount { get; set; }

    [Params(16, 2000, 10000, 100000)]
    public int ValueLength { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < FieldCount; i++)
        {
            builder.Append("--").Append(Boundary).Append("\r\n");
            builder.Append("Content-Disposition: form-data; name=\"field").Append(i).Append("\"\r\n\r\n");
            builder.Append('a', ValueLength).Append("\r\n");
        }
        builder.Append("--").Append(Boundary).Append("--\r\n");
        _stream = new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString()));
    }

    [Benchmark]
    public async Task<int> ReadSectionsAsString()
    {
        _stream.Position = 0;
        var reader = new MultipartReader(Boundary, _stream);
        var length = 0;
        while (await reader.ReadNextSectionAsync() is { } section)
        {
            length += (await section.ReadAsStringAsync(CancellationToken.None)).Length;
        }
        return length;
    }
}

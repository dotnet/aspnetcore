// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Http;

[MemoryDiagnoser]
public class HttpRequestJsonContentTypeBenchmarks
{
    private DefaultHttpContext _json;
    private DefaultHttpContext _jsonUtf8;
    private DefaultHttpContext _problem;
    private MemoryStream _body;

    [GlobalSetup]
    public void Setup()
    {
        _json = new DefaultHttpContext();
        _json.Request.ContentType = "application/json";
        _jsonUtf8 = new DefaultHttpContext();
        _jsonUtf8.Request.ContentType = "application/json; charset=utf-8";
        _problem = new DefaultHttpContext();
        _problem.Request.ContentType = "application/problem+json";
        _body = new MemoryStream(Encoding.UTF8.GetBytes("{\"name\":\"a\",\"value\":1}"));
        _jsonUtf8.Request.Body = _body;
    }

    [Benchmark]
    public bool Json() => _json.Request.HasJsonContentType();

    [Benchmark]
    public bool JsonUtf8() => _jsonUtf8.Request.HasJsonContentType();

    [Benchmark]
    public bool ProblemJson() => _problem.Request.HasJsonContentType();

    [Benchmark]
    public async Task<Item> ReadFromJson()
    {
        _body.Position = 0;
        return await _jsonUtf8.Request.ReadFromJsonAsync<Item>();
    }

    public class Item
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }
}

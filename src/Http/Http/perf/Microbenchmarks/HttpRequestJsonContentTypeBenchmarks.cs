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
    private byte[] _body;

    [GlobalSetup]
    public void Setup()
    {
        _json = new DefaultHttpContext();
        _json.Request.ContentType = "application/json";
        _jsonUtf8 = new DefaultHttpContext();
        _jsonUtf8.Request.ContentType = "application/json; charset=utf-8";
        _problem = new DefaultHttpContext();
        _problem.Request.ContentType = "application/problem+json";
        _body = Encoding.UTF8.GetBytes("{\"name\":\"a\",\"value\":1}");
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
        // A fresh request each time so the body (and its cached BodyReader) isn't already consumed.
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json; charset=utf-8";
        context.Request.Body = new MemoryStream(_body);
        var item = await context.Request.ReadFromJsonAsync<Item>();
        if (item?.Value != 1)
        {
            throw new InvalidOperationException("Unexpected result.");
        }
        return item;
    }

    public class Item
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }
}

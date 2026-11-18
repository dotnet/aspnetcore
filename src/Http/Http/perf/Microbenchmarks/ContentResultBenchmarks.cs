// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http;

[MemoryDiagnoser]
public class ContentResultBenchmarks
{
    private IServiceProvider _services = default!;
    private readonly IResult _html = TypedResults.Content("<h1>Hello</h1>", "text/html");
    private readonly IResult _textUtf8 = TypedResults.Text("Hello", "text/plain; charset=utf-8");
    private readonly IResult _default = TypedResults.Text("Hello");

    [GlobalSetup]
    public void Setup() => _services = new ServiceCollection().AddLogging().BuildServiceProvider();

    private DefaultHttpContext NewContext()
    {
        var ctx = new DefaultHttpContext { RequestServices = _services };
        ctx.Response.Body = Stream.Null;
        return ctx;
    }

    [Benchmark]
    public Task Html() => _html.ExecuteAsync(NewContext());

    [Benchmark]
    public Task TextUtf8() => _textUtf8.ExecuteAsync(NewContext());

    [Benchmark]
    public Task Default() => _default.ExecuteAsync(NewContext());
}

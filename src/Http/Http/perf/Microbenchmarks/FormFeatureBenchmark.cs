// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Http;

public class FormFeatureBenchmark
{
    [Params(null, "application/json; charset=utf-8", "application/x-www-form-urlencoded", "multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq")]
    public string ContentType { get; set; }

    [Benchmark(Baseline = true)]
    public DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = ContentType;
        return context;
    }

    [Benchmark]
    public bool HasFormContentType()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = ContentType;
        return context.Request.HasFormContentType;
    }
}

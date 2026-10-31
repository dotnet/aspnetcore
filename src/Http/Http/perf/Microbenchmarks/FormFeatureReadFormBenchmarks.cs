// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Http;

[MemoryDiagnoser]
public class FormFeatureReadFormBenchmarks
{
    private byte[] _urlEncoded;
    private byte[] _multipart;
    private byte[] _multipart3;
    private byte[] _multipartLarge;
    private const string Boundary = "----WebKitFormBoundarymx2fSWqWSd0OxQqq";

    [GlobalSetup]
    public void Setup()
    {
        _urlEncoded = Encoding.UTF8.GetBytes("name=value&foo=bar&baz=qux");
        _multipart3 = Encoding.UTF8.GetBytes($"--{Boundary}\r\nContent-Disposition: form-data; name=\"name\"\r\n\r\nvalue\r\n--{Boundary}\r\nContent-Disposition: form-data; name=\"email\"\r\n\r\nsomeone@example.com\r\n--{Boundary}\r\nContent-Disposition: form-data; name=\"comment\"\r\n\r\nThis is a somewhat longer comment value for the form.\r\n--{Boundary}--\r\n");
        _multipartLarge = Encoding.UTF8.GetBytes($"--{Boundary}\r\nContent-Disposition: form-data; name=\"big\"\r\n\r\n{new string('x', 8000)}\r\n--{Boundary}--\r\n");
        _multipart = Encoding.UTF8.GetBytes($"--{Boundary}\r\nContent-Disposition: form-data; name=\"name\"\r\n\r\nvalue\r\n--{Boundary}--\r\n");
    }

    [Benchmark]
    public async Task<int> UrlEncoded()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Body = new MemoryStream(_urlEncoded);
        if (!context.Request.HasFormContentType)
        {
            return 0;
        }
        var form = await context.Request.ReadFormAsync();
        return form.Count;
    }

    [Benchmark]
    public async Task<int> Multipart()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "multipart/form-data; boundary=" + Boundary;
        context.Request.Body = new MemoryStream(_multipart);
        if (!context.Request.HasFormContentType)
        {
            return 0;
        }
        var form = await context.Request.ReadFormAsync();
        return form.Count;
    }

    [Benchmark]
    public async Task<int> Multipart3()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "multipart/form-data; boundary=" + Boundary;
        context.Request.Body = new MemoryStream(_multipart3);
        if (!context.Request.HasFormContentType)
        {
            return 0;
        }
        var form = await context.Request.ReadFormAsync();
        return form.Count;
    }

    [Benchmark]
    public async Task<int> MultipartLarge()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "multipart/form-data; boundary=" + Boundary;
        context.Request.Body = new MemoryStream(_multipartLarge);
        var form = await context.Request.ReadFormAsync();
        return form.Count;
    }
}

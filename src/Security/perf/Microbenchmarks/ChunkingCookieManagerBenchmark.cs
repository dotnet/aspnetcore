// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Security;

public class ChunkingCookieManagerBenchmark
{
    private ChunkingCookieManager _chunkingCookieManager;
    private HttpContext _httpContext;
    private CookieOptions _cookieOptions;
    private string _stringToAdd;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _chunkingCookieManager = new ChunkingCookieManager()
        {
            ChunkSize = 86
        };

        _httpContext = new DefaultHttpContext();

        _cookieOptions = new CookieOptions()
        {
            Domain = "foo.com",
            Path = "/",
            Secure = true
        };

        _httpContext.Request.Headers["Cookie"] = new[]
        {
                "TestCookie=chunks-7",
                "TestCookieC1=abcdefghi",
                "TestCookieC2=jklmnopqr",
                "TestCookieC3=stuvwxyz0",
                "TestCookieC4=123456789",
                "TestCookieC5=ABCDEFGHI",
                "TestCookieC6=JKLMNOPQR",
                "TestCookieC7=STUVWXYZ"
            };

        _stringToAdd = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    }

    [Benchmark]
    public void AppendCookies()
    {
        _chunkingCookieManager.AppendResponseCookie(_httpContext, "TestCookie1", _stringToAdd, _cookieOptions);
        _httpContext.Response.Headers[HeaderNames.SetCookie] = StringValues.Empty;
    }

    [Benchmark]
    public void DeleteCookies()
    {
        _chunkingCookieManager.DeleteCookie(_httpContext, "TestCookie", _cookieOptions);
        _httpContext.Response.Headers[HeaderNames.SetCookie] = StringValues.Empty;
    }
}

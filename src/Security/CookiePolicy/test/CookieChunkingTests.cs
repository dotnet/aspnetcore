// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Internal;

public class CookieChunkingTests
{
    [Fact]
    public void AppendLargeCookie_Appended()
    {
        HttpContext context = new DefaultHttpContext();

        string testString = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        new ChunkingCookieManager() { ChunkSize = null }.AppendResponseCookie(context, "TestCookie", testString, new CookieOptions());
        var values = context.Response.Headers["Set-Cookie"];
        Assert.Single(values);
        Assert.Equal("TestCookie=" + testString + "; path=/", values[0]);
    }

    [Fact]
    public void AppendLargeCookie_WithOptions_Appended()
    {
        HttpContext context = new DefaultHttpContext();
        var now = DateTimeOffset.UtcNow;
        var options = new CookieOptions
        {
            Domain = "foo.com",
            HttpOnly = true,
            SameSite = Http.SameSiteMode.Strict,
            Path = "/bar",
            Secure = true,
            Expires = now.AddMinutes(5),
            MaxAge = TimeSpan.FromMinutes(5)
        };
        var testString = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        new ChunkingCookieManager() { ChunkSize = null }.AppendResponseCookie(context, "TestCookie", testString, options);

        var values = context.Response.Headers["Set-Cookie"];
        Assert.Single(values);
        Assert.Equal($"TestCookie={testString}; expires={now.AddMinutes(5).ToString("R")}; max-age=300; domain=foo.com; path=/bar; secure; samesite=strict; httponly", values[0]);
    }

    [Fact]
    public void AppendLargeCookieWithLimit_Chunked()
    {
        HttpContext context = new DefaultHttpContext();

        string testString = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        new ChunkingCookieManager() { ChunkSize = 44 }.AppendResponseCookie(context, "TestCookie", testString, new CookieOptions());
        var values = context.Response.Headers["Set-Cookie"];
        Assert.Equal(4, values.Count);
        Assert.Equal<string[]>(new[]
        {
                "TestCookie=chunks-3; path=/",
                "TestCookieC1=abcdefghijklmnopqrstuv; path=/",
                "TestCookieC2=wxyz0123456789ABCDEFGH; path=/",
                "TestCookieC3=IJKLMNOPQRSTUVWXYZ; path=/",
            }, values);
    }

    [Fact]
    public void AppendLargeCookieWithExtensions_Chunked()
    {
        HttpContext context = new DefaultHttpContext();

        string testString = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        new ChunkingCookieManager() { ChunkSize = 63 }.AppendResponseCookie(context, "TestCookie", testString,
            new CookieOptions() { Extensions = { "simple", "key=value" } });
        var values = context.Response.Headers["Set-Cookie"];
        Assert.Equal(4, values.Count);
        Assert.Equal<string[]>(new[]
        {
                "TestCookie=chunks-3; path=/; simple; key=value",
                "TestCookieC1=abcdefghijklmnopqrstuv; path=/; simple; key=value",
                "TestCookieC2=wxyz0123456789ABCDEFGH; path=/; simple; key=value",
                "TestCookieC3=IJKLMNOPQRSTUVWXYZ; path=/; simple; key=value",
            }, values);
    }

    [Fact]
    public void AppendSmallerCookieThanPriorValue_SingleChunk_DeletesOldChunks()
    {
        HttpContext context = new DefaultHttpContext();
        context.Request.Headers["Cookie"] = new[]
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

        new ChunkingCookieManager() { ChunkSize = 31 }.AppendResponseCookie(context, "TestCookie", "ShortValue", new CookieOptions());
        var values = context.Response.Headers["Set-Cookie"];
        Assert.Equal<string[]>(
        [
            "TestCookieC1=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/",
            "TestCookieC2=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/",
            "TestCookieC3=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/",
            "TestCookieC4=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/",
            "TestCookieC5=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/",
            "TestCookieC6=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/",
            "TestCookieC7=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/",
            "TestCookie=ShortValue; path=/",
        ], values);
    }

    [Fact]
    public void AppendSmallerCookieThanPriorValue_MultipleChunks_DeletesOldChunks()
    {
        HttpContext context = new DefaultHttpContext();
        context.Request.Headers["Cookie"] = new[]
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

        new ChunkingCookieManager() { ChunkSize = 31 }.AppendResponseCookie(context, "TestCookie", "abcdefghijklmnopqr", new CookieOptions());
        var values = context.Response.Headers["Set-Cookie"];
        Assert.Equal<string[]>(
        [
            "TestCookieC3=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/",
            "TestCookieC4=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/",
            "TestCookieC5=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/",
            "TestCookieC6=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/",
            "TestCookieC7=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/",
            "TestCookie=chunks-2; path=/",
            "TestCookieC1=abcdefghi; path=/",
            "TestCookieC2=jklmnopqr; path=/",
        ], values);
    }

    [Fact]
    public void GetLargeChunkedCookie_Reassembled()
    {
        HttpContext context = new DefaultHttpContext();
        context.Request.Headers["Cookie"] = new[]
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

        string result = new ChunkingCookieManager().GetRequestCookie(context, "TestCookie");
        string testString = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        Assert.Equal(testString, result);
    }

    [Fact]
    public void GetLargeChunkedCookieWithMissingChunk_ThrowingEnabled_Throws()
    {
        HttpContext context = new DefaultHttpContext();
        context.Request.Headers["Cookie"] = new[]
        {
                "TestCookie=chunks-7",
                "TestCookieC1=abcdefghi",
                // Missing chunk "TestCookieC2=jklmnopqr",
                "TestCookieC3=stuvwxyz0",
                "TestCookieC4=123456789",
                "TestCookieC5=ABCDEFGHI",
                "TestCookieC6=JKLMNOPQR",
                "TestCookieC7=STUVWXYZ"
            };

        Assert.Throws<FormatException>(() => new ChunkingCookieManager() { ThrowForPartialCookies = true }
            .GetRequestCookie(context, "TestCookie"));
    }

    [Fact]
    public void GetLargeChunkedCookieWithMissingChunk_ThrowingDisabled_NotReassembled()
    {
        HttpContext context = new DefaultHttpContext();
        context.Request.Headers["Cookie"] = new[]
        {
                "TestCookie=chunks-7",
                "TestCookieC1=abcdefghi",
                // Missing chunk "TestCookieC2=jklmnopqr",
                "TestCookieC3=stuvwxyz0",
                "TestCookieC4=123456789",
                "TestCookieC5=ABCDEFGHI",
                "TestCookieC6=JKLMNOPQR",
                "TestCookieC7=STUVWXYZ"
            };

        string result = new ChunkingCookieManager() { ThrowForPartialCookies = false }.GetRequestCookie(context, "TestCookie");
        string testString = "chunks-7";
        Assert.Equal(testString, result);
    }

    [Fact]
    public void DeleteChunkedCookieWithOptions_AllDeleted()
    {
        HttpContext context = new DefaultHttpContext();
        context.Request.Headers.Append("Cookie", "TestCookie=chunks-7;TestCookieC1=1;TestCookieC2=2;TestCookieC3=3;TestCookieC4=4;TestCookieC5=5;TestCookieC6=6;TestCookieC7=7");

        new ChunkingCookieManager().DeleteCookie(context, "TestCookie", new CookieOptions() { Domain = "foo.com", Secure = true, Extensions = { "extension" } });
        var cookies = context.Response.Headers["Set-Cookie"];
        Assert.Equal(8, cookies.Count);
        Assert.Equal(new[]
        {
            "TestCookie=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure; extension",
            "TestCookieC1=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure; extension",
            "TestCookieC2=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure; extension",
            "TestCookieC3=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure; extension",
            "TestCookieC4=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure; extension",
            "TestCookieC5=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure; extension",
            "TestCookieC6=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure; extension",
            "TestCookieC7=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure; extension",
        }, cookies);
    }

    [Fact]
    public void DeleteChunkedCookieWithMissingRequestCookies_OnlyPresentCookiesDeleted()
    {
        HttpContext context = new DefaultHttpContext();
        context.Request.Headers.Append("Cookie", "TestCookie=chunks-7;TestCookieC1=1;TestCookieC2=2");

        new ChunkingCookieManager().DeleteCookie(context, "TestCookie", new CookieOptions() { Domain = "foo.com", Secure = true });
        var cookies = context.Response.Headers["Set-Cookie"];
        Assert.Equal(3, cookies.Count);
        Assert.Equal(new[]
        {
            "TestCookie=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure",
            "TestCookieC1=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure",
            "TestCookieC2=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure",
        }, cookies);
    }

    [Fact]
    public void DeleteChunkedCookieWithMissingRequestCookies_StopsAtMissingChunk()
    {
        HttpContext context = new DefaultHttpContext();
        // C3 is missing so we don't try to delete C4 either.
        context.Request.Headers.Append("Cookie", "TestCookie=chunks-7;TestCookieC1=1;TestCookieC2=2;TestCookieC4=4");

        new ChunkingCookieManager().DeleteCookie(context, "TestCookie", new CookieOptions() { Domain = "foo.com", Secure = true });
        var cookies = context.Response.Headers["Set-Cookie"];
        Assert.Equal(3, cookies.Count);
        Assert.Equal(new[]
        {
            "TestCookie=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure",
            "TestCookieC1=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure",
            "TestCookieC2=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure",
        }, cookies);
    }

    [Fact]
    public void DeleteChunkedCookieWithOptionsAndResponseCookies_AllDeleted()
    {
        var chunkingCookieManager = new ChunkingCookieManager();
        HttpContext httpContext = new DefaultHttpContext();

        httpContext.Request.Headers["Cookie"] = new[]
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

        var cookieOptions = new CookieOptions()
        {
            Domain = "foo.com",
            Path = "/",
            Secure = true,
            Extensions = { "extension" }
        };

        httpContext.Response.Headers[HeaderNames.SetCookie] = new[]
        {
                "TestCookie=chunks-7; domain=foo.com; path=/; secure; other=extension",
                "TestCookieC1=STUVWXYZ; domain=foo.com; path=/; secure",
                "TestCookieC2=123456789; domain=foo.com; path=/; secure",
                "TestCookieC3=stuvwxyz0; domain=foo.com; path=/; secure",
                "TestCookieC4=123456789; domain=foo.com; path=/; secure",
                "TestCookieC5=ABCDEFGHI; domain=foo.com; path=/; secure",
                "TestCookieC6=JKLMNOPQR; domain=foo.com; path=/; secure",
                "TestCookieC7=STUVWXYZ; domain=foo.com; path=/; secure"
            };

        chunkingCookieManager.DeleteCookie(httpContext, "TestCookie", cookieOptions);
        Assert.Equal(8, httpContext.Response.Headers[HeaderNames.SetCookie].Count);
        Assert.Equal(new[]
        {
                "TestCookie=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure; extension",
                "TestCookieC1=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure; extension",
                "TestCookieC2=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure; extension",
                "TestCookieC3=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure; extension",
                "TestCookieC4=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure; extension",
                "TestCookieC5=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure; extension",
                "TestCookieC6=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure; extension",
                "TestCookieC7=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure; extension"
            }, httpContext.Response.Headers[HeaderNames.SetCookie]);
    }
}

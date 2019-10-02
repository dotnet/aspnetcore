// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Internal
{
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
                SameSite = SameSiteMode.Strict,
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
            context.Request.Headers.Append("Cookie", "TestCookie=chunks-7");

            new ChunkingCookieManager().DeleteCookie(context, "TestCookie", new CookieOptions() { Domain = "foo.com", Secure = true });
            var cookies = context.Response.Headers["Set-Cookie"];
            Assert.Equal(8, cookies.Count);
            Assert.Equal(new[]
            {
                "TestCookie=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure",
                "TestCookieC1=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure",
                "TestCookieC2=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure",
                "TestCookieC3=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure",
                "TestCookieC4=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure",
                "TestCookieC5=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure",
                "TestCookieC6=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure",
                "TestCookieC7=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; secure",
            }, cookies);
        }
    }
}

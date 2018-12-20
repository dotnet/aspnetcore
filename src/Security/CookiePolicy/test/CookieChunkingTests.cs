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
            Assert.Equal("TestCookie=" + testString + "; path=/; samesite=lax", values[0]);
        }

        [Fact]
        public void AppendLargeCookieWithLimit_Chunked()
        {
            HttpContext context = new DefaultHttpContext();

            string testString = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            new ChunkingCookieManager() { ChunkSize = 44 }.AppendResponseCookie(context, "TestCookie", testString, new CookieOptions());
            var values = context.Response.Headers["Set-Cookie"];
            Assert.Equal(9, values.Count);
            Assert.Equal<string[]>(new[]
            {
                "TestCookie=chunks-8; path=/; samesite=lax",
                "TestCookieC1=abcdefgh; path=/; samesite=lax",
                "TestCookieC2=ijklmnop; path=/; samesite=lax",
                "TestCookieC3=qrstuvwx; path=/; samesite=lax",
                "TestCookieC4=yz012345; path=/; samesite=lax",
                "TestCookieC5=6789ABCD; path=/; samesite=lax",
                "TestCookieC6=EFGHIJKL; path=/; samesite=lax",
                "TestCookieC7=MNOPQRST; path=/; samesite=lax",
                "TestCookieC8=UVWXYZ; path=/; samesite=lax",
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

            new ChunkingCookieManager().DeleteCookie(context, "TestCookie", new CookieOptions() { Domain = "foo.com" });
            var cookies = context.Response.Headers["Set-Cookie"];
            Assert.Equal(8, cookies.Count);
            Assert.Equal(new[]
            {
                "TestCookie=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; samesite=lax",
                "TestCookieC1=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; samesite=lax",
                "TestCookieC2=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; samesite=lax",
                "TestCookieC3=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; samesite=lax",
                "TestCookieC4=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; samesite=lax",
                "TestCookieC5=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; samesite=lax",
                "TestCookieC6=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; samesite=lax",
                "TestCookieC7=; expires=Thu, 01 Jan 1970 00:00:00 GMT; domain=foo.com; path=/; samesite=lax",
            }, cookies);
        }
    }
}

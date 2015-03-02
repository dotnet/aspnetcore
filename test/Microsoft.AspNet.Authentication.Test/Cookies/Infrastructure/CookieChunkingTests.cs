// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core;
using Xunit;

namespace Microsoft.AspNet.Authentication.Cookies.Infrastructure
{
    public class CookieChunkingTests
    {
        [Fact]
        public void AppendLargeCookie_Appended()
        {
            HttpContext context = new DefaultHttpContext();

            string testString = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            new ChunkingCookieManager() { ChunkSize = null }.AppendResponseCookie(context, "TestCookie", testString, new CookieOptions());
            IList<string> values = context.Response.Headers.GetValues("Set-Cookie");
            Assert.Equal(1, values.Count);
            Assert.Equal("TestCookie=" + testString + "; path=/", values[0]);
        }

        [Fact]
        public void AppendLargeCookieWithLimit_Chunked()
        {
            HttpContext context = new DefaultHttpContext();

            string testString = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            new ChunkingCookieManager() { ChunkSize = 30 }.AppendResponseCookie(context, "TestCookie", testString, new CookieOptions());
            IList<string> values = context.Response.Headers.GetValues("Set-Cookie");
            Assert.Equal(9, values.Count);
            Assert.Equal(new[]
            {
                "TestCookie=chunks:8; path=/",
                "TestCookieC1=abcdefgh; path=/",
                "TestCookieC2=ijklmnop; path=/",
                "TestCookieC3=qrstuvwx; path=/",
                "TestCookieC4=yz012345; path=/",
                "TestCookieC5=6789ABCD; path=/",
                "TestCookieC6=EFGHIJKL; path=/",
                "TestCookieC7=MNOPQRST; path=/",
                "TestCookieC8=UVWXYZ; path=/",
            }, values);
        }

        [Fact]
        public void AppendLargeQuotedCookieWithLimit_QuotedChunked()
        {
            HttpContext context = new DefaultHttpContext();

            string testString = "\"abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ\"";
            new ChunkingCookieManager() { ChunkSize = 32 }.AppendResponseCookie(context, "TestCookie", testString, new CookieOptions());
            IList<string> values = context.Response.Headers.GetValues("Set-Cookie");
            Assert.Equal(9, values.Count);
            Assert.Equal(new[]
            {
                "TestCookie=chunks:8; path=/",
                "TestCookieC1=\"abcdefgh\"; path=/",
                "TestCookieC2=\"ijklmnop\"; path=/",
                "TestCookieC3=\"qrstuvwx\"; path=/",
                "TestCookieC4=\"yz012345\"; path=/",
                "TestCookieC5=\"6789ABCD\"; path=/",
                "TestCookieC6=\"EFGHIJKL\"; path=/",
                "TestCookieC7=\"MNOPQRST\"; path=/",
                "TestCookieC8=\"UVWXYZ\"; path=/",
            }, values);
        }

        [Fact]
        public void GetLargeChunkedCookie_Reassembled()
        {
            HttpContext context = new DefaultHttpContext();
            context.Request.Headers.AppendValues("Cookie",
                "TestCookie=chunks:7",
                "TestCookieC1=abcdefghi",
                "TestCookieC2=jklmnopqr",
                "TestCookieC3=stuvwxyz0",
                "TestCookieC4=123456789",
                "TestCookieC5=ABCDEFGHI",
                "TestCookieC6=JKLMNOPQR",
                "TestCookieC7=STUVWXYZ");

            string result = new ChunkingCookieManager().GetRequestCookie(context, "TestCookie");
            string testString = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            Assert.Equal(testString, result);
        }

        [Fact]
        public void GetLargeChunkedCookieWithQuotes_Reassembled()
        {
            HttpContext context = new DefaultHttpContext();
            context.Request.Headers.AppendValues("Cookie",
                "TestCookie=chunks:7",
                "TestCookieC1=\"abcdefghi\"",
                "TestCookieC2=\"jklmnopqr\"",
                "TestCookieC3=\"stuvwxyz0\"",
                "TestCookieC4=\"123456789\"",
                "TestCookieC5=\"ABCDEFGHI\"",
                "TestCookieC6=\"JKLMNOPQR\"",
                "TestCookieC7=\"STUVWXYZ\"");

            string result = new ChunkingCookieManager().GetRequestCookie(context, "TestCookie");
            string testString = "\"abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ\"";
            Assert.Equal(testString, result);
        }

        [Fact]
        public void GetLargeChunkedCookieWithMissingChunk_ThrowingEnabled_Throws()
        {
            HttpContext context = new DefaultHttpContext();
            context.Request.Headers.AppendValues("Cookie",
                "TestCookie=chunks:7",
                "TestCookieC1=abcdefghi",
                // Missing chunk "TestCookieC2=jklmnopqr",
                "TestCookieC3=stuvwxyz0",
                "TestCookieC4=123456789",
                "TestCookieC5=ABCDEFGHI",
                "TestCookieC6=JKLMNOPQR",
                "TestCookieC7=STUVWXYZ");

            Assert.Throws<FormatException>(() => new ChunkingCookieManager().GetRequestCookie(context, "TestCookie"));
        }

        [Fact]
        public void GetLargeChunkedCookieWithMissingChunk_ThrowingDisabled_NotReassembled()
        {
            HttpContext context = new DefaultHttpContext();
            context.Request.Headers.AppendValues("Cookie",
                "TestCookie=chunks:7",
                "TestCookieC1=abcdefghi",
                // Missing chunk "TestCookieC2=jklmnopqr",
                "TestCookieC3=stuvwxyz0",
                "TestCookieC4=123456789",
                "TestCookieC5=ABCDEFGHI",
                "TestCookieC6=JKLMNOPQR",
                "TestCookieC7=STUVWXYZ");

            string result = new ChunkingCookieManager() { ThrowForPartialCookies = false }.GetRequestCookie(context, "TestCookie");
            string testString = "chunks:7";
            Assert.Equal(testString, result);
        }

        [Fact]
        public void DeleteChunkedCookieWithOptions_AllDeleted()
        {
            HttpContext context = new DefaultHttpContext();
            context.Request.Headers.AppendValues("Cookie", "TestCookie=chunks:7");

            new ChunkingCookieManager().DeleteCookie(context, "TestCookie", new CookieOptions() { Domain = "foo.com" });
            var cookies = context.Response.Headers.GetValues("Set-Cookie");
            Assert.Equal(8, cookies.Count);
            Assert.Equal(new[]
            {
                "TestCookie=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT",
                "TestCookieC1=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT",
                "TestCookieC2=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT",
                "TestCookieC3=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT",
                "TestCookieC4=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT",
                "TestCookieC5=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT",
                "TestCookieC6=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT",
                "TestCookieC7=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT",
            }, cookies);
        }
    }
}

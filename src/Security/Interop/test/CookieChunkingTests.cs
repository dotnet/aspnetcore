// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Owin.Security.Interop
{
    public class CookieChunkingTests
    {
        [Fact]
        public void AppendLargeCookie_Appended()
        {
            var context = new OwinContext();

            string testString = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            new ChunkingCookieManager() { ChunkSize = null }.AppendResponseCookie(context, "TestCookie", testString, new CookieOptions() { SameSite = SameSiteMode.Lax });
            var values = context.Response.Headers["Set-Cookie"];
            Assert.Equal("TestCookie=" + testString + "; path=/; SameSite=Lax", values);
        }

        [Fact]
        public void AppendLargeCookie_WithOptions_Appended()
        {
            var context = new OwinContext();
            var now = DateTime.UtcNow;
            var options = new CookieOptions
            {
                Domain = "foo.com",
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Path = "/bar",
                Secure = true,
                Expires = now.AddMinutes(5),
            };
            var testString = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            new ChunkingCookieManager() { ChunkSize = null }.AppendResponseCookie(context, "TestCookie", testString, options);

            var values = context.Response.Headers["Set-Cookie"];
            Assert.Equal($"TestCookie={testString}; domain=foo.com; path=/bar; expires={now.AddMinutes(5).ToString("ddd, dd-MMM-yyyy HH:mm:ss ")}GMT; secure; HttpOnly; SameSite=Strict", values);
        }

        [Fact]
        public void AppendLargeCookieWithLimit_Chunked()
        {
            var context = new OwinContext();

            string testString = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            new ChunkingCookieManager() { ChunkSize = 44 }.AppendResponseCookie(context, "TestCookie", testString, new CookieOptions() { SameSite = SameSiteMode.Lax });
            Assert.True(context.Response.Headers.TryGetValue("Set-Cookie", out var values));
            Assert.Equal(9, values.Length);
            Assert.Equal<string[]>(new[]
            {
                "TestCookie=chunks-8; path=/; SameSite=Lax",
                "TestCookieC1=abcdefgh; path=/; SameSite=Lax",
                "TestCookieC2=ijklmnop; path=/; SameSite=Lax",
                "TestCookieC3=qrstuvwx; path=/; SameSite=Lax",
                "TestCookieC4=yz012345; path=/; SameSite=Lax",
                "TestCookieC5=6789ABCD; path=/; SameSite=Lax",
                "TestCookieC6=EFGHIJKL; path=/; SameSite=Lax",
                "TestCookieC7=MNOPQRST; path=/; SameSite=Lax",
                "TestCookieC8=UVWXYZ; path=/; SameSite=Lax",
            }, values);
        }

        [Fact]
        public void GetLargeChunkedCookie_Reassembled()
        {
            var context = new OwinContext();
            context.Request.Headers.Add("Cookie", new[]
            {
                "TestCookie=chunks-7",
                "TestCookieC1=abcdefghi",
                "TestCookieC2=jklmnopqr",
                "TestCookieC3=stuvwxyz0",
                "TestCookieC4=123456789",
                "TestCookieC5=ABCDEFGHI",
                "TestCookieC6=JKLMNOPQR",
                "TestCookieC7=STUVWXYZ"
            });

            string result = new ChunkingCookieManager().GetRequestCookie(context, "TestCookie");
            string testString = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            Assert.Equal(testString, result);
        }

        [Fact]
        public void GetLargeChunkedCookieWithMissingChunk_ThrowingEnabled_Throws()
        {
            var context = new OwinContext();
            context.Request.Headers.Add("Cookie", new[]
            {
                "TestCookie=chunks-7",
                "TestCookieC1=abcdefghi",
                // Missing chunk "TestCookieC2=jklmnopqr",
                "TestCookieC3=stuvwxyz0",
                "TestCookieC4=123456789",
                "TestCookieC5=ABCDEFGHI",
                "TestCookieC6=JKLMNOPQR",
                "TestCookieC7=STUVWXYZ"
            });

            Assert.Throws<FormatException>(() => new ChunkingCookieManager() { ThrowForPartialCookies = true }
                .GetRequestCookie(context, "TestCookie"));
        }

        [Fact]
        public void GetLargeChunkedCookieWithMissingChunk_ThrowingDisabled_NotReassembled()
        {
            var context = new OwinContext();
            context.Request.Headers.Add("Cookie", new[]
            {
                "TestCookie=chunks-7",
                "TestCookieC1=abcdefghi",
                // Missing chunk "TestCookieC2=jklmnopqr",
                "TestCookieC3=stuvwxyz0",
                "TestCookieC4=123456789",
                "TestCookieC5=ABCDEFGHI",
                "TestCookieC6=JKLMNOPQR",
                "TestCookieC7=STUVWXYZ"
            });

            string result = new ChunkingCookieManager() { ThrowForPartialCookies = false }.GetRequestCookie(context, "TestCookie");
            string testString = "chunks-7";
            Assert.Equal(testString, result);
        }

        [Fact]
        public void DeleteChunkedCookieWithOptions_AllDeleted()
        {
            var context = new OwinContext();
            context.Request.Headers.Append("Cookie", "TestCookie=chunks-7;TestCookieC1=1;TestCookieC2=2;TestCookieC3=3;TestCookieC4=4;TestCookieC5=5;TestCookieC6=6;TestCookieC7=7");

            new ChunkingCookieManager().DeleteCookie(context, "TestCookie", new CookieOptions() { Domain = "foo.com", Secure = true, SameSite = SameSiteMode.Lax });
            Assert.True(context.Response.Headers.TryGetValue("Set-Cookie", out var cookies));
            Assert.Equal(8, cookies.Length);
            Assert.Equal(new[]
            {
                "TestCookie=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
                "TestCookieC1=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
                "TestCookieC2=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
                "TestCookieC3=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
                "TestCookieC4=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
                "TestCookieC5=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
                "TestCookieC6=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
                "TestCookieC7=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
            }, cookies);
        }

        [Fact]
        public void DeleteChunkedCookieWithMissingRequestCookies_OnlyPresentCookiesDeleted()
        {
            var context = new OwinContext();
            context.Request.Headers.Append("Cookie", "TestCookie=chunks-7;TestCookieC1=1;TestCookieC2=2");

            new ChunkingCookieManager().DeleteCookie(context, "TestCookie", new CookieOptions() { Domain = "foo.com", Secure = true, SameSite = SameSiteMode.Lax });
            Assert.True(context.Response.Headers.TryGetValue("Set-Cookie", out var cookies));
            Assert.Equal(3, cookies.Length);
            Assert.Equal(new[]
            {
                "TestCookie=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
                "TestCookieC1=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
                "TestCookieC2=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
            }, cookies);
        }

        [Fact]
        public void DeleteChunkedCookieWithMissingRequestCookies_StopsAtMissingChunk()
        {
            var context = new OwinContext();
            // C3 is missing so we don't try to delete C4 either.
            context.Request.Headers.Append("Cookie", "TestCookie=chunks-7;TestCookieC1=1;TestCookieC2=2;TestCookieC4=4");

            new ChunkingCookieManager().DeleteCookie(context, "TestCookie", new CookieOptions() { Domain = "foo.com", Secure = true, SameSite = SameSiteMode.Lax });
            Assert.True(context.Response.Headers.TryGetValue("Set-Cookie", out var cookies));
            Assert.Equal(3, cookies.Length);
            Assert.Equal(new[]
            {
                "TestCookie=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
                "TestCookieC1=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
                "TestCookieC2=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
            }, cookies);
        }

        [Fact]
        public void DeleteChunkedCookieWithOptionsAndResponseCookies_AllDeleted()
        {
            var chunkingCookieManager = new ChunkingCookieManager();
            var context = new OwinContext();

            context.Request.Headers.Add("Cookie", new[]
            {
                "TestCookie=chunks-7",
                "TestCookieC1=abcdefghi",
                "TestCookieC2=jklmnopqr",
                "TestCookieC3=stuvwxyz0",
                "TestCookieC4=123456789",
                "TestCookieC5=ABCDEFGHI",
                "TestCookieC6=JKLMNOPQR",
                "TestCookieC7=STUVWXYZ"
            });

            var cookieOptions = new CookieOptions()
            {
                Domain = "foo.com",
                Path = "/",
                Secure = true,
                SameSite = SameSiteMode.Lax
            };

            context.Response.Headers.Add("Set-Cookie", new[]
            {
                "TestCookie=chunks-7; domain=foo.com; path=/; secure; SameSite=Lax",
                "TestCookieC1=STUVWXYZ; domain=foo.com; path=/; secure; SameSite=Lax",
                "TestCookieC2=123456789; domain=foo.com; path=/; secure; SameSite=Lax",
                "TestCookieC3=stuvwxyz0; domain=foo.com; path=/; secure; SameSite=Lax",
                "TestCookieC4=123456789; domain=foo.com; path=/; secure; SameSite=Lax",
                "TestCookieC5=ABCDEFGHI; domain=foo.com; path=/; secure; SameSite=Lax",
                "TestCookieC6=JKLMNOPQR; domain=foo.com; path=/; secure; SameSite=Lax",
                "TestCookieC7=STUVWXYZ; domain=foo.com; path=/; secure; SameSite=Lax"
            });

            chunkingCookieManager.DeleteCookie(context, "TestCookie", cookieOptions);
            Assert.True(context.Response.Headers.TryGetValue("Set-Cookie", out var cookies));
            Assert.Equal(8, cookies.Length);
            Assert.Equal(new[]
            {
                "TestCookie=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
                "TestCookieC1=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
                "TestCookieC2=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
                "TestCookieC3=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
                "TestCookieC4=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
                "TestCookieC5=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
                "TestCookieC6=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax",
                "TestCookieC7=; domain=foo.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT; secure; SameSite=Lax"
            }, cookies);
        }
    }
}

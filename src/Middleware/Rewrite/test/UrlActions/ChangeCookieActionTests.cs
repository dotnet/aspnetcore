// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.UrlActions;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.UrlActions
{
    public class ChangeCookieActionTests
    {
        [Fact]
        public void SetsCookie()
        {
            var now = DateTimeOffset.UtcNow;
            var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
            var action = new ChangeCookieAction("Cookie", () => now)
            {
                Value = "Chocolate Chip",
                Domain = "contoso.com",
                Lifetime = TimeSpan.FromMinutes(1440),
                Path = "/recipes",
                Secure = true,
                HttpOnly = true
            };

            action.ApplyAction(context, null, null);

            var cookieHeaders = context.HttpContext.Response.Headers[HeaderNames.SetCookie];
            var header = Assert.Single(cookieHeaders);
            Assert.Equal($"Cookie=Chocolate%20Chip; expires={HeaderUtilities.FormatDate(now.AddMinutes(1440))}; domain=contoso.com; path=/recipes; secure; httponly", header);
        }

        [Fact]
        public void ZeroLifetime()
        {
            var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
            var action = new ChangeCookieAction("Cookie")
            {
                Value = "Chocolate Chip",
            };

            action.ApplyAction(context, null, null);

            var cookieHeaders = context.HttpContext.Response.Headers[HeaderNames.SetCookie];
            var header = Assert.Single(cookieHeaders);
            Assert.Equal($"Cookie=Chocolate%20Chip", header);
        }


        [Fact]
        public void UnsetCookie()
        {
            var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
            var action = new ChangeCookieAction("Cookie");

            action.ApplyAction(context, null, null);

            var cookieHeaders = context.HttpContext.Response.Headers[HeaderNames.SetCookie];
            var header = Assert.Single(cookieHeaders);
            Assert.Equal($"Cookie=", header);
        }
    }
}

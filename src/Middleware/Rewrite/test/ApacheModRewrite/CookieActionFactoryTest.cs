// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Rewrite.ApacheModRewrite;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Test
{
    public class CookieActionFactoryTest
    {
        [Fact]
        public void Creates_OneCookie()
        {
            var cookie = new CookieActionFactory().Create("NAME:VALUE:DOMAIN:1440:path:secure:httponly");

            Assert.Equal("NAME", cookie.Name);
            Assert.Equal("VALUE", cookie.Value);
            Assert.Equal("DOMAIN", cookie.Domain);
            Assert.Equal(TimeSpan.FromMinutes(1440), cookie.Lifetime);
            Assert.Equal("path", cookie.Path);
            Assert.True(cookie.Secure);
            Assert.True(cookie.HttpOnly);
        }

        [Fact]
        public void Creates_OneCookie_AltSeparator()
        {
            var action = new CookieActionFactory().Create(";NAME;VALUE:WithColon;DOMAIN;1440;path;secure;httponly");

            Assert.Equal("NAME", action.Name);
            Assert.Equal("VALUE:WithColon", action.Value);
            Assert.Equal("DOMAIN", action.Domain);
            Assert.Equal(TimeSpan.FromMinutes(1440), action.Lifetime);
            Assert.Equal("path", action.Path);
            Assert.True(action.Secure);
            Assert.True(action.HttpOnly);
        }

        [Fact]
        public void Creates_HttpOnly()
        {
            var action = new CookieActionFactory().Create(";NAME;VALUE;DOMAIN;;;;httponly");

            Assert.Equal("NAME", action.Name);
            Assert.Equal("VALUE", action.Value);
            Assert.Equal("DOMAIN", action.Domain);
            Assert.Equal(0, action.Lifetime.TotalSeconds);
            Assert.Equal(string.Empty, action.Path);
            Assert.False(action.Secure);
            Assert.True(action.HttpOnly);
        }

        [Theory]
        [InlineData("NAME::", "", null)]
        [InlineData("NAME::domain", "", "domain")]
        [InlineData("NAME:VALUE:;", "VALUE", null)] // special case with dangling ';'
        [InlineData("NAME:value:", "value", null)]
        [InlineData(" NAME :  v  :  ", "v", null)] // trims values
        public void TrimsValues(string flagValue, string value, string domain)
        {
            var factory = new CookieActionFactory();
            var action = factory.Create(flagValue);
            Assert.Equal("NAME", action.Name);
            Assert.NotNull(action.Value);
            Assert.Equal(value, action.Value);
            Assert.Equal(domain, action.Domain);
        }

        [Theory]
        [InlineData("NAME")] // missing value and domain
        [InlineData("NAME:   ")] // missing  domain
        [InlineData("NAME:VALUE")] // missing domain
        [InlineData(";NAME;VAL:UE")] // missing domain
        public void ThrowsForInvalidFormat(string flagValue)
        {
            var factory = new CookieActionFactory();
            var ex = Assert.Throws<FormatException>(() => factory.Create(flagValue));
            Assert.Equal(Resources.FormatError_InvalidChangeCookieFlag(flagValue), ex.Message);
        }

        [Theory]
        [InlineData("bad_number")]
        [InlineData("-1")]
        [InlineData("0.9")]
        public void ThrowsForInvalidIntFormat(string badInt)
        {
            var factory = new CookieActionFactory();
            var ex = Assert.Throws<FormatException>(() => factory.Create("NAME:VALUE:DOMAIN:" + badInt));
            Assert.Equal(Resources.FormatError_CouldNotParseInteger(badInt), ex.Message);
        }
    }
}
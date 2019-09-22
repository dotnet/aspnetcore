// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public class UrlPrefixTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("5000")]
        [InlineData("//noscheme")]
        public void CreateThrowsForUrlsWithoutSchemeDelimiter(string url)
        {
            Assert.Throws<FormatException>(() => UrlPrefix.Create(url));
        }

        [Theory]
        [InlineData("://emptyscheme")]
        [InlineData("://")]
        [InlineData("://:5000")]
        public void CreateThrowsForUrlsWithEmptyScheme(string url)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => UrlPrefix.Create(url));
        }

        [Theory]
        [InlineData("http://")]
        [InlineData("http://:5000")]
        [InlineData("http:///")]
        [InlineData("http:///:5000")]
        [InlineData("http:////")]
        [InlineData("http:////:5000")]
        public void CreateThrowsForUrlsWithoutHost(string url)
        {
            Assert.Throws<ArgumentNullException>(() => UrlPrefix.Create(url));
        }

        [Theory]
        [InlineData("http://www.example.com:NOTAPORT")]
        [InlineData("https://www.example.com:NOTAPORT")]
        [InlineData("http://www.example.com:NOTAPORT/")]
        [InlineData("http://foo:/tmp/httpsys-test.sock:5000/doesn't/matter")]
        public void CreateThrowsForUrlsWithInvalidPorts(string url)
        {
            Assert.Throws<FormatException>(() => UrlPrefix.Create(url));
        }

        [Theory]
        [InlineData("http://+", "http", "+", "80", "/", "http://+:80/")]
        [InlineData("http://*", "http", "*", "80", "/", "http://*:80/")]
        [InlineData("http://localhost", "http", "localhost", "80", "/", "http://localhost:80/")]
        [InlineData("http://www.example.com", "http", "www.example.com", "80", "/", "http://www.example.com:80/")]
        [InlineData("https://www.example.com", "https", "www.example.com", "443", "/", "https://www.example.com:443/")]
        [InlineData("http://www.example.com/", "http", "www.example.com", "80", "/", "http://www.example.com:80/")]
        [InlineData("http://www.example.com/foo?bar=baz", "http", "www.example.com", "80", "/foo?bar=baz/", "http://www.example.com:80/foo?bar=baz/")]
        [InlineData("http://www.example.com:5000", "http", "www.example.com", "5000", "/", "http://www.example.com:5000/")]
        [InlineData("https://www.example.com:5000", "https", "www.example.com", "5000", "/", "https://www.example.com:5000/")]
        [InlineData("http://www.example.com:5000/", "http", "www.example.com", "5000", "/", "http://www.example.com:5000/")]
        [InlineData("http://www.example.com/foo:bar", "http", "www.example.com", "80", "/foo:bar/", "http://www.example.com:80/foo:bar/")]
        public void UrlsAreParsedCorrectly(string url, string scheme, string host, string port, string pathBase, string toString)
        {
            var urlPrefix = UrlPrefix.Create(url);

            Assert.Equal(scheme, urlPrefix.Scheme);
            Assert.Equal(host, urlPrefix.Host);
            Assert.Equal(port, urlPrefix.Port);
            Assert.Equal(pathBase, urlPrefix.Path);

            Assert.Equal(toString ?? url, urlPrefix.ToString());
        }

        [Fact]
        public void PathBaseIsNotNormalized()
        {
            var urlPrefix = UrlPrefix.Create("http://localhost:8080/p\u0041\u030Athbase");

            Assert.False(urlPrefix.Path.IsNormalized(NormalizationForm.FormC));
            Assert.Equal("/p\u0041\u030Athbase/", urlPrefix.Path);
        }
    }
}

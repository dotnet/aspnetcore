// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class ServerAddressFacts
    {
        [Theory]
        [InlineData("")]
        [InlineData("//noscheme")]
        public void FromUriReturnsNullForSchemelessUrls(string url)
        {
            Assert.Null(ServerAddress.FromUrl(url));
        }

        [Theory]
        [InlineData("://emptyscheme", "", "emptyscheme", 0, "")]
        [InlineData("http://localhost", "http", "localhost", 80, "")]
        [InlineData("http://www.example.com", "http", "www.example.com", 80, "")]
        [InlineData("https://www.example.com", "https", "www.example.com", 443, "")]
        [InlineData("http://www.example.com/", "http", "www.example.com", 80, "")]
        [InlineData("http://www.example.com/foo?bar=baz", "http", "www.example.com", 80, "/foo?bar=baz")]
        [InlineData("http://www.example.com:5000", "http", "www.example.com", 5000, "")]
        [InlineData("https://www.example.com:5000", "https", "www.example.com", 5000, "")]
        [InlineData("http://www.example.com:5000/", "http", "www.example.com", 5000, "")]
        [InlineData("http://www.example.com:NOTAPORT", "http", "www.example.com:NOTAPORT", 80, "")]
        [InlineData("https://www.example.com:NOTAPORT", "https", "www.example.com:NOTAPORT", 443, "")]
        [InlineData("http://www.example.com:NOTAPORT/", "http", "www.example.com:NOTAPORT", 80, "")]
        [InlineData("http://foo:/tmp/kestrel-test.sock:5000/doesn't/matter", "http", "foo:", 80, "/tmp/kestrel-test.sock:5000/doesn't/matter")]
        [InlineData("http://unix:foo/tmp/kestrel-test.sock", "http", "unix:foo", 80, "/tmp/kestrel-test.sock")]
        [InlineData("http://unix:5000/tmp/kestrel-test.sock", "http", "unix", 5000, "/tmp/kestrel-test.sock")]
        [InlineData("http://unix:/tmp/kestrel-test.sock", "http", "unix:/tmp/kestrel-test.sock", 0, "")]
        [InlineData("https://unix:/tmp/kestrel-test.sock", "https", "unix:/tmp/kestrel-test.sock", 0, "")]
        [InlineData("http://unix:/tmp/kestrel-test.sock:", "http", "unix:/tmp/kestrel-test.sock", 0, "")]
        [InlineData("http://unix:/tmp/kestrel-test.sock:/", "http", "unix:/tmp/kestrel-test.sock", 0, "")]
        [InlineData("http://unix:/tmp/kestrel-test.sock:5000/doesn't/matter", "http", "unix:/tmp/kestrel-test.sock", 0, "5000/doesn't/matter")]
        public void UrlsAreParsedCorrectly(string url, string scheme, string host, int port, string pathBase)
        {
            var serverAddress = ServerAddress.FromUrl(url);

            Assert.Equal(scheme, serverAddress.Scheme);
            Assert.Equal(host, serverAddress.Host);
            Assert.Equal(port, serverAddress.Port);
            Assert.Equal(pathBase, serverAddress.PathBase);
        }

        [Fact]
        public void PathBaseIsNormalized()
        {
            var serverAddres = ServerAddress.FromUrl("http://localhost:8080/p\u0041\u030Athbase");

            Assert.True(serverAddres.PathBase.IsNormalized(NormalizationForm.FormC));
            Assert.Equal("/p\u00C5thbase", serverAddres.PathBase);
        }
    }
}

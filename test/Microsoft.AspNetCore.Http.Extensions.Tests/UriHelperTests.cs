// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Http.Extensions
{
    public class UriHelperTests
    {
        [Fact]
        public void EncodeEmptyPartialUrl()
        {
            var result = UriHelper.Encode();

            Assert.Equal("/", result);
        }

        [Fact]
        public void EncodePartialUrl()
        {
            var result = UriHelper.Encode(new PathString("/un?escaped/base"), new PathString("/un?escaped"),
                new QueryString("?name=val%23ue"), new FragmentString("#my%20value"));

            Assert.Equal("/un%3Fescaped/base/un%3Fescaped?name=val%23ue#my%20value", result);
        }

        [Fact]
        public void EncodeEmptyFullUrl()
        {
            var result = UriHelper.Encode("http", new HostString(string.Empty));

            Assert.Equal("http:///", result);
        }

        [Fact]
        public void EncodeFullUrl()
        {
            var result = UriHelper.Encode("http", new HostString("my.HoΨst:80"), new PathString("/un?escaped/base"), new PathString("/un?escaped"),
                new QueryString("?name=val%23ue"), new FragmentString("#my%20value"));

            Assert.Equal("http://my.xn--host-cpd:80/un%3Fescaped/base/un%3Fescaped?name=val%23ue#my%20value", result);
        }

        [Fact]
        public void GetEncodedUrlFromRequest()
        {
            var request = new DefaultHttpContext().Request;
            request.Scheme = "http";
            request.Host = new HostString("my.HoΨst:80");
            request.PathBase = new PathString("/un?escaped/base");
            request.Path = new PathString("/un?escaped");
            request.QueryString = new QueryString("?name=val%23ue");

            Assert.Equal("http://my.xn--host-cpd:80/un%3Fescaped/base/un%3Fescaped?name=val%23ue", request.GetEncodedUrl());
        }

        [Fact]
        public void GetDisplayUrlFromRequest()
        {
            var request = new DefaultHttpContext().Request;
            request.Scheme = "http";
            request.Host = new HostString("my.HoΨst:80");
            request.PathBase = new PathString("/un?escaped/base");
            request.Path = new PathString("/un?escaped");
            request.QueryString = new QueryString("?name=val%23ue");

            Assert.Equal("http://my.hoψst:80/un?escaped/base/un?escaped?name=val%23ue", request.GetDisplayUrl());
        }
    }
}

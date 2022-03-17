// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Extensions;

public class UriHelperTests
{
    [Fact]
    public void EncodeEmptyPartialUrl()
    {
        var result = UriHelper.BuildRelative();

        Assert.Equal("/", result);
    }

    [Fact]
    public void EncodePartialUrl()
    {
        var result = UriHelper.BuildRelative(new PathString("/un?escaped/base"), new PathString("/un?escaped"),
            new QueryString("?name=val%23ue"), new FragmentString("#my%20value"));

        Assert.Equal("/un%3Fescaped/base/un%3Fescaped?name=val%23ue#my%20value", result);
    }

    [Fact]
    public void EncodeEmptyFullUrl()
    {
        var result = UriHelper.BuildAbsolute("http", new HostString(string.Empty));

        Assert.Equal("http:///", result);
    }

    [Fact]
    public void EncodeFullUrl()
    {
        var result = UriHelper.BuildAbsolute("http", new HostString("my.HoΨst:80"), new PathString("/un?escaped/base"), new PathString("/un?escaped"),
            new QueryString("?name=val%23ue"), new FragmentString("#my%20value"));

        Assert.Equal("http://my.xn--host-cpd:80/un%3Fescaped/base/un%3Fescaped?name=val%23ue#my%20value", result);
    }

    [Theory]
    [InlineData("http", "example.com", "", "", "", "", "http://example.com/")]
    [InlineData("https", "example.com", "", "", "", "", "https://example.com/")]
    [InlineData("http", "example.com", "", "/foo/bar", "", "", "http://example.com/foo/bar")]
    [InlineData("http", "example.com", "", "/foo/bar", "?baz=1", "", "http://example.com/foo/bar?baz=1")]
    [InlineData("http", "example.com", "", "/foo", "", "#col=2", "http://example.com/foo#col=2")]
    [InlineData("http", "example.com", "", "/foo", "?bar=1", "#col=2", "http://example.com/foo?bar=1#col=2")]
    [InlineData("http", "example.com", "/base", "/foo", "?bar=1", "#col=2", "http://example.com/base/foo?bar=1#col=2")]
    [InlineData("http", "example.com", "/base/", "/foo", "?bar=1", "#col=2", "http://example.com/base/foo?bar=1#col=2")]
    [InlineData("http", "example.com", "/base/", "", "?bar=1", "#col=2", "http://example.com/base/?bar=1#col=2")]
    [InlineData("http", "example.com", "", "", "?bar=1", "#col=2", "http://example.com/?bar=1#col=2")]
    [InlineData("http", "example.com", "", "", "", "#frag?stillfrag/stillfrag", "http://example.com/#frag?stillfrag/stillfrag")]
    [InlineData("http", "example.com", "", "", "?q/stillq", "#frag?stillfrag/stillfrag", "http://example.com/?q/stillq#frag?stillfrag/stillfrag")]
    [InlineData("http", "example.com", "", "/fo#o", "", "#col=2", "http://example.com/fo%23o#col=2")]
    [InlineData("http", "example.com", "", "/fo?o", "", "#col=2", "http://example.com/fo%3Fo#col=2")]
    [InlineData("ftp", "example.com", "", "/", "", "", "ftp://example.com/")]
    [InlineData("ftp", "example.com", "/", "/", "", "", "ftp://example.com/")]
    [InlineData("https", "127.0.0.0:80", "", "/bar", "", "", "https://127.0.0.0:80/bar")]
    [InlineData("http", "[1080:0:0:0:8:800:200C:417A]", "", "/index.html", "", "", "http://[1080:0:0:0:8:800:200C:417A]/index.html")]
    [InlineData("http", "example.com", "", "///", "", "", "http://example.com///")]
    [InlineData("http", "example.com", "///", "///", "", "", "http://example.com/////")]
    public void BuildAbsoluteGenerationChecks(
        string scheme,
        string host,
        string pathBase,
        string path,
        string query,
        string fragment,
        string expectedUri)
    {
        var uri = UriHelper.BuildAbsolute(
            scheme,
            new HostString(host),
            new PathString(pathBase),
            new PathString(path),
            new QueryString(query),
            new FragmentString(fragment));

        Assert.Equal(expectedUri, uri);
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

    [Theory]
    [InlineData("/un?escaped/base")]
    [InlineData(null)]
    public void GetDisplayUrlFromRequest(string pathBase)
    {
        var request = new DefaultHttpContext().Request;
        request.Scheme = "http";
        request.Host = new HostString("my.HoΨst:80");
        request.PathBase = new PathString(pathBase);
        request.Path = new PathString("/un?escaped");
        request.QueryString = new QueryString("?name=val%23ue");

        Assert.Equal("http://my.hoψst:80" + pathBase + "/un?escaped?name=val%23ue", request.GetDisplayUrl());
    }

    [Theory]
    [InlineData("http://example.com", "http", "example.com", "", "", "")]
    [InlineData("https://example.com", "https", "example.com", "", "", "")]
    [InlineData("http://example.com/foo/bar", "http", "example.com", "/foo/bar", "", "")]
    [InlineData("http://example.com/foo/bar?baz=1", "http", "example.com", "/foo/bar", "?baz=1", "")]
    [InlineData("http://example.com/foo#col=2", "http", "example.com", "/foo", "", "#col=2")]
    [InlineData("http://example.com/foo?bar=1#col=2", "http", "example.com", "/foo", "?bar=1", "#col=2")]
    [InlineData("http://example.com?bar=1#col=2", "http", "example.com", "", "?bar=1", "#col=2")]
    [InlineData("http://example.com#frag?stillfrag/stillfrag", "http", "example.com", "", "", "#frag?stillfrag/stillfrag")]
    [InlineData("http://example.com?q/stillq#frag?stillfrag/stillfrag", "http", "example.com", "", "?q/stillq", "#frag?stillfrag/stillfrag")]
    [InlineData("http://example.com/fo%23o#col=2", "http", "example.com", "/fo#o", "", "#col=2")]
    [InlineData("http://example.com/fo%3Fo#col=2", "http", "example.com", "/fo?o", "", "#col=2")]
    [InlineData("ftp://example.com/", "ftp", "example.com", "/", "", "")]
    [InlineData("https://127.0.0.0:80/bar", "https", "127.0.0.0:80", "/bar", "", "")]
    [InlineData("http://[1080:0:0:0:8:800:200C:417A]/index.html", "http", "[1080:0:0:0:8:800:200C:417A]", "/index.html", "", "")]
    [InlineData("http://example.com///", "http", "example.com", "///", "", "")]
    public void FromAbsoluteUriParsingChecks(
        string uri,
        string expectedScheme,
        string expectedHost,
        string expectedPath,
        string expectedQuery,
        string expectedFragment)
    {
        string scheme = null;
        var host = new HostString();
        var path = new PathString();
        var query = new QueryString();
        var fragment = new FragmentString();
        UriHelper.FromAbsolute(uri, out scheme, out host, out path, out query, out fragment);

        Assert.Equal(scheme, expectedScheme);
        Assert.Equal(host, new HostString(expectedHost));
        Assert.Equal(path, new PathString(expectedPath));
        Assert.Equal(query, new QueryString(expectedQuery));
        Assert.Equal(fragment, new FragmentString(expectedFragment));
    }

    [Fact]
    public void FromAbsoluteToBuildAbsolute()
    {
        var scheme = "http";
        var host = new HostString("example.com");
        var path = new PathString("/index.html");
        var query = new QueryString("?foo=1");
        var fragment = new FragmentString("#col=1");
        var request = UriHelper.BuildAbsolute(scheme, host, path: path, query: query, fragment: fragment);

        string resScheme = null;
        var resHost = new HostString();
        var resPath = new PathString();
        var resQuery = new QueryString();
        var resFragment = new FragmentString();
        UriHelper.FromAbsolute(request, out resScheme, out resHost, out resPath, out resQuery, out resFragment);

        Assert.Equal(scheme, resScheme);
        Assert.Equal(host, resHost);
        Assert.Equal(path, resPath);
        Assert.Equal(query, resQuery);
        Assert.Equal(fragment, resFragment);
    }

    [Fact]
    public void BuildAbsoluteNullInputThrowsArgumentNullException()
    {
        var resHost = new HostString();
        var resPath = new PathString();
        var resQuery = new QueryString();
        var resFragment = new FragmentString();
        Assert.Throws<ArgumentNullException>(() => UriHelper.BuildAbsolute(null, resHost, resPath, resPath, resQuery, resFragment));

    }

    [Fact]
    public void FromAbsoluteNullInputThrowsArgumentNullException()
    {
        string resScheme = null;
        var resHost = new HostString();
        var resPath = new PathString();
        var resQuery = new QueryString();
        var resFragment = new FragmentString();
        Assert.Throws<ArgumentNullException>(() => UriHelper.FromAbsolute(null, out resScheme, out resHost, out resPath, out resQuery, out resFragment));

    }
}

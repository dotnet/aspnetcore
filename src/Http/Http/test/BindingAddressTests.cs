// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Http.Tests;

public class BindingAddressTests
{
    [Theory]
    [InlineData("")]
    [InlineData("5000")]
    [InlineData("//noscheme")]
    public void FromUriThrowsForUrlsWithoutSchemeDelimiter(string url)
    {
        Assert.Throws<FormatException>(() => BindingAddress.Parse(url));
    }

    [Theory]
    [InlineData("://")]
    [InlineData("://:5000")]
    [InlineData("http://")]
    [InlineData("http://:5000")]
    [InlineData("http:///")]
    [InlineData("http:///:5000")]
    [InlineData("http:////")]
    [InlineData("http:////:5000")]
    public void FromUriThrowsForUrlsWithoutHost(string url)
    {
        Assert.Throws<FormatException>(() => BindingAddress.Parse(url));
    }

    [ConditionalTheory]
    [InlineData("http://unix:/")]
    [InlineData("http://unix:/c")]
    [InlineData("http://unix:/wrong.path")]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "Windows has drive letters and volume separator (c:), testing this url on unix or osx provides completely different output.")]
    public void FromUriThrowsForUrlsWithWrongFilePathOnWindows(string url)
    {
        Assert.Throws<FormatException>(() => BindingAddress.Parse(url));
    }

    [Theory]
    [InlineData("://emptyscheme", "", "emptyscheme", 0, "", "://emptyscheme:0")]
    [InlineData("http://+", "http", "+", 80, "", "http://+:80")]
    [InlineData("http://*", "http", "*", 80, "", "http://*:80")]
    [InlineData("http://localhost", "http", "localhost", 80, "", "http://localhost:80")]
    [InlineData("http://www.example.com", "http", "www.example.com", 80, "", "http://www.example.com:80")]
    [InlineData("https://www.example.com", "https", "www.example.com", 443, "", "https://www.example.com:443")]
    [InlineData("http://www.example.com/", "http", "www.example.com", 80, "", "http://www.example.com:80")]
    [InlineData("http://www.example.com/foo?bar=baz", "http", "www.example.com", 80, "/foo?bar=baz", "http://www.example.com:80/foo?bar=baz")]
    [InlineData("http://www.example.com:5000", "http", "www.example.com", 5000, "", null)]
    [InlineData("https://www.example.com:5000", "https", "www.example.com", 5000, "", null)]
    [InlineData("http://www.example.com:5000/", "http", "www.example.com", 5000, "", "http://www.example.com:5000")]
    [InlineData("http://www.example.com:NOTAPORT", "http", "www.example.com:NOTAPORT", 80, "", "http://www.example.com:notaport:80")]
    [InlineData("https://www.example.com:NOTAPORT", "https", "www.example.com:NOTAPORT", 443, "", "https://www.example.com:notaport:443")]
    [InlineData("http://www.example.com:NOTAPORT/", "http", "www.example.com:NOTAPORT", 80, "", "http://www.example.com:notaport:80")]
    [InlineData("http://foo:/tmp/kestrel-test.sock:5000/doesn't/matter", "http", "foo:", 80, "/tmp/kestrel-test.sock:5000/doesn't/matter", "http://foo::80/tmp/kestrel-test.sock:5000/doesn't/matter")]
    [InlineData("http://unix:foo/tmp/kestrel-test.sock", "http", "unix:foo", 80, "/tmp/kestrel-test.sock", "http://unix:foo:80/tmp/kestrel-test.sock")]
    [InlineData("http://unix:5000/tmp/kestrel-test.sock", "http", "unix", 5000, "/tmp/kestrel-test.sock", "http://unix:5000/tmp/kestrel-test.sock")]
    public void UrlsAreParsedCorrectly(string url, string scheme, string host, int port, string pathBase, string toString)
    {
        var serverAddress = BindingAddress.Parse(url);

        Assert.Equal(scheme, serverAddress.Scheme);
        Assert.Equal(host, serverAddress.Host);
        Assert.Equal(port, serverAddress.Port);
        Assert.Equal(pathBase, serverAddress.PathBase);

        Assert.Equal(toString ?? url, serverAddress.ToString());
    }

    [ConditionalTheory]
    [InlineData("http://unix:/tmp/kestrel-test.sock", "http", "unix:/tmp/kestrel-test.sock", 0, "", null)]
    [InlineData("https://unix:/tmp/kestrel-test.sock", "https", "unix:/tmp/kestrel-test.sock", 0, "", null)]
    [InlineData("http://unix:/tmp/kestrel-test.sock:", "http", "unix:/tmp/kestrel-test.sock", 0, "", "http://unix:/tmp/kestrel-test.sock")]
    [InlineData("http://unix:/tmp/kestrel-test.sock:/", "http", "unix:/tmp/kestrel-test.sock", 0, "", "http://unix:/tmp/kestrel-test.sock")]
    [InlineData("http://unix:/tmp/kestrel-test.sock:5000/doesn't/matter", "http", "unix:/tmp/kestrel-test.sock", 0, "5000/doesn't/matter", "http://unix:/tmp/kestrel-test.sock")]
    [OSSkipCondition(OperatingSystems.Windows)]
    public void UnixSocketUrlsAreParsedCorrectlyOnUnix(string url, string scheme, string host, int port, string pathBase, string toString)
    {
        var serverAddress = BindingAddress.Parse(url);

        Assert.Equal(scheme, serverAddress.Scheme);
        Assert.Equal(host, serverAddress.Host);
        Assert.Equal(port, serverAddress.Port);
        Assert.Equal(pathBase, serverAddress.PathBase);

        Assert.Equal(toString ?? url, serverAddress.ToString());
    }

    [ConditionalTheory]
    [InlineData("http://unix:/c:/foo/bar/pipe.socket", "http", "unix:/c:/foo/bar/pipe.socket", 0, "", null)]
    [InlineData("http://unix:/c:/foo/bar/pipe.socket:", "http", "unix:/c:/foo/bar/pipe.socket", 0, "", "http://unix:/c:/foo/bar/pipe.socket")]
    [InlineData("http://unix:/c:/foo/bar/pipe.socket:/", "http", "unix:/c:/foo/bar/pipe.socket", 0, "", "http://unix:/c:/foo/bar/pipe.socket")]
    [InlineData("http://unix:/c:/foo/bar/pipe.socket:5000/doesn't/matter", "http", "unix:/c:/foo/bar/pipe.socket", 0, "5000/doesn't/matter", "http://unix:/c:/foo/bar/pipe.socket")]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "Windows has drive letters and volume separator (c:), testing this url on unix or osx provides completely different output.")]
    public void UnixSocketUrlsAreParsedCorrectlyOnWindows(string url, string scheme, string host, int port, string pathBase, string toString)
    {
        var serverAddress = BindingAddress.Parse(url);

        Assert.Equal(scheme, serverAddress.Scheme);
        Assert.Equal(host, serverAddress.Host);
        Assert.Equal(port, serverAddress.Port);
        Assert.Equal(pathBase, serverAddress.PathBase);

        Assert.Equal(toString ?? url, serverAddress.ToString());
    }

}

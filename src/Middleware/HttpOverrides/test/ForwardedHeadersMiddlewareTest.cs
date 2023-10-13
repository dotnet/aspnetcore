// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.HttpOverrides;

public class ForwardedHeadersMiddlewareTests
{
    [Fact]
    public async Task XForwardedForDefaultSettingsChangeRemoteIpAndPort()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-For"] = "11.111.111.11:9090";
        });

        Assert.Equal("11.111.111.11", context.Connection.RemoteIpAddress.ToString());
        Assert.Equal(9090, context.Connection.RemotePort);
        // No Original set if RemoteIpAddress started null.
        Assert.False(context.Request.Headers.ContainsKey("X-Original-For"));
        // Should have been consumed and removed
        Assert.False(context.Request.Headers.ContainsKey("X-Forwarded-For"));
    }

    [Theory]
    [InlineData(1, "11.111.111.11.12345", "10.0.0.1", 99)] // Invalid
    public async Task XForwardedForFirstValueIsInvalid(int limit, string header, string expectedIp, int expectedPort)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor,
                        ForwardLimit = limit,
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-For"] = header;
            c.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
            c.Connection.RemotePort = 99;
        });

        Assert.Equal(expectedIp, context.Connection.RemoteIpAddress.ToString());
        Assert.Equal(expectedPort, context.Connection.RemotePort);
        Assert.False(context.Request.Headers.ContainsKey("X-Original-For"));
        Assert.True(context.Request.Headers.ContainsKey("X-Forwarded-For"));
        Assert.Equal(header, context.Request.Headers["X-Forwarded-For"]);
    }

    [Theory]
    [InlineData(1, "11.111.111.11:12345", "11.111.111.11", 12345, "", false)]
    [InlineData(1, "11.111.111.11:12345", "11.111.111.11", 12345, "", true)]
    [InlineData(10, "11.111.111.11:12345", "11.111.111.11", 12345, "", false)]
    [InlineData(10, "11.111.111.11:12345", "11.111.111.11", 12345, "", true)]
    [InlineData(1, "12.112.112.12:23456, 11.111.111.11:12345", "11.111.111.11", 12345, "12.112.112.12:23456", false)]
    [InlineData(1, "12.112.112.12:23456, 11.111.111.11:12345", "11.111.111.11", 12345, "12.112.112.12:23456", true)]
    [InlineData(2, "12.112.112.12:23456, 11.111.111.11:12345", "12.112.112.12", 23456, "", false)]
    [InlineData(2, "12.112.112.12:23456, 11.111.111.11:12345", "12.112.112.12", 23456, "", true)]
    [InlineData(10, "12.112.112.12:23456, 11.111.111.11:12345", "12.112.112.12", 23456, "", false)]
    [InlineData(10, "12.112.112.12:23456, 11.111.111.11:12345", "12.112.112.12", 23456, "", true)]
    [InlineData(10, "12.112.112.12.23456, 11.111.111.11:12345", "11.111.111.11", 12345, "12.112.112.12.23456", false)] // Invalid 2nd value
    [InlineData(10, "12.112.112.12.23456, 11.111.111.11:12345", "11.111.111.11", 12345, "12.112.112.12.23456", true)] // Invalid 2nd value
    [InlineData(10, "13.113.113.13:34567, 12.112.112.12.23456, 11.111.111.11:12345", "11.111.111.11", 12345, "13.113.113.13:34567,12.112.112.12.23456", false)] // Invalid 2nd value
    [InlineData(10, "13.113.113.13:34567, 12.112.112.12.23456, 11.111.111.11:12345", "11.111.111.11", 12345, "13.113.113.13:34567,12.112.112.12.23456", true)] // Invalid 2nd value
    [InlineData(2, "13.113.113.13:34567, 12.112.112.12:23456, 11.111.111.11:12345", "12.112.112.12", 23456, "13.113.113.13:34567", false)]
    [InlineData(2, "13.113.113.13:34567, 12.112.112.12:23456, 11.111.111.11:12345", "12.112.112.12", 23456, "13.113.113.13:34567", true)]
    [InlineData(3, "13.113.113.13:34567, 12.112.112.12:23456, 11.111.111.11:12345", "13.113.113.13", 34567, "", false)]
    [InlineData(3, "13.113.113.13:34567, 12.112.112.12:23456, 11.111.111.11:12345", "13.113.113.13", 34567, "", true)]
    public async Task XForwardedForForwardLimit(int limit, string header, string expectedIp, int expectedPort, string remainingHeader, bool requireSymmetry)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    var options = new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor,
                        RequireHeaderSymmetry = requireSymmetry,
                        ForwardLimit = limit,
                    };
                    options.KnownProxies.Clear();
                    options.KnownNetworks.Clear();
                    app.UseForwardedHeaders(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-For"] = header;
            c.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
            c.Connection.RemotePort = 99;
        });

        Assert.Equal(expectedIp, context.Connection.RemoteIpAddress.ToString());
        Assert.Equal(expectedPort, context.Connection.RemotePort);
        Assert.Equal(remainingHeader, context.Request.Headers["X-Forwarded-For"].ToString());
    }

    [Theory]
    [InlineData("11.111.111.11", false)]
    [InlineData("127.0.0.1", true)]
    [InlineData("127.0.1.1", true)]
    [InlineData("::1", true)]
    [InlineData("::", false)]
    public async Task XForwardedForLoopback(string originalIp, bool expectForwarded)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor,
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-For"] = "10.0.0.1:1234";
            c.Connection.RemoteIpAddress = IPAddress.Parse(originalIp);
            c.Connection.RemotePort = 99;
        });

        if (expectForwarded)
        {
            Assert.Equal("10.0.0.1", context.Connection.RemoteIpAddress.ToString());
            Assert.Equal(1234, context.Connection.RemotePort);
            Assert.True(context.Request.Headers.ContainsKey("X-Original-For"));
            Assert.Equal(new IPEndPoint(IPAddress.Parse(originalIp), 99).ToString(),
                context.Request.Headers["X-Original-For"]);
        }
        else
        {
            Assert.Equal(originalIp, context.Connection.RemoteIpAddress.ToString());
            Assert.Equal(99, context.Connection.RemotePort);
            Assert.False(context.Request.Headers.ContainsKey("X-Original-For"));
        }
    }

    [Theory]
    [InlineData(1, "11.111.111.11:12345", "20.0.0.1", "10.0.0.1", 99, false)]
    [InlineData(1, "11.111.111.11:12345", "20.0.0.1", "10.0.0.1", 99, true)]
    [InlineData(1, "", "10.0.0.1", "10.0.0.1", 99, false)]
    [InlineData(1, "", "10.0.0.1", "10.0.0.1", 99, true)]
    [InlineData(1, "11.111.111.11:12345", "10.0.0.1", "11.111.111.11", 12345, false)]
    [InlineData(1, "11.111.111.11:12345", "10.0.0.1", "11.111.111.11", 12345, true)]
    [InlineData(1, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1", "11.111.111.11", 12345, false)]
    [InlineData(1, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1", "11.111.111.11", 12345, true)]
    [InlineData(1, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11", "11.111.111.11", 12345, false)]
    [InlineData(1, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11", "11.111.111.11", 12345, true)]
    [InlineData(2, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11", "12.112.112.12", 23456, false)]
    [InlineData(2, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11", "12.112.112.12", 23456, true)]
    [InlineData(1, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "11.111.111.11", 12345, false)]
    [InlineData(1, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "11.111.111.11", 12345, true)]
    [InlineData(2, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "12.112.112.12", 23456, false)]
    [InlineData(2, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "12.112.112.12", 23456, true)]
    [InlineData(3, "13.113.113.13:34567, 12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "13.113.113.13", 34567, false)]
    [InlineData(3, "13.113.113.13:34567, 12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "13.113.113.13", 34567, true)]
    [InlineData(3, "13.113.113.13:34567, 12.112.112.12;23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "11.111.111.11", 12345, false)] // Invalid 2nd IP
    [InlineData(3, "13.113.113.13:34567, 12.112.112.12;23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "11.111.111.11", 12345, true)] // Invalid 2nd IP
    [InlineData(3, "13.113.113.13;34567, 12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "12.112.112.12", 23456, false)] // Invalid 3rd IP
    [InlineData(3, "13.113.113.13;34567, 12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "12.112.112.12", 23456, true)] // Invalid 3rd IP
    public async Task XForwardedForForwardKnownIps(int limit, string header, string knownIPs, string expectedIp, int expectedPort, bool requireSymmetry)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    var options = new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor,
                        RequireHeaderSymmetry = requireSymmetry,
                        ForwardLimit = limit,
                    };
                    foreach (var ip in knownIPs.Split(',').Select(text => IPAddress.Parse(text)))
                    {
                        options.KnownProxies.Add(ip);
                    }
                    app.UseForwardedHeaders(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-For"] = header;
            c.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
            c.Connection.RemotePort = 99;
        });

        Assert.Equal(expectedIp, context.Connection.RemoteIpAddress.ToString());
        Assert.Equal(expectedPort, context.Connection.RemotePort);
    }

    [Fact]
    public async Task XForwardedForOverrideBadIpDoesntChangeRemoteIp()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-For"] = "BAD-IP";
        });

        Assert.Null(context.Connection.RemoteIpAddress);
    }

    [Fact]
    public async Task XForwardedHostOverrideChangesRequestHost()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedHost
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Host"] = "testhost";
        });

        Assert.Equal("testhost", context.Request.Host.ToString());
    }

    public static TheoryData<string> HostHeaderData
    {
        get
        {
            return new TheoryData<string>() {
                    "z",
                    "1",
                    "y:1",
                    "1:1",
                    "[ABCdef]",
                    "[abcDEF]:0",
                    "[abcdef:127.2355.1246.114]:0",
                    "[::1]:80",
                    "127.0.0.1:80",
                    "900.900.900.900:9523547852",
                    "foo",
                    "foo:234",
                    "foo.bar.baz",
                    "foo.BAR.baz:46245",
                    "foo.ba-ar.baz:46245",
                    "-foo:1234",
                    "xn--c1yn36f:134",
                    "-",
                    "_",
                    "~",
                    "!",
                    "$",
                    "'",
                    "(",
                    ")",
                };
        }
    }

    [Theory]
    [MemberData(nameof(HostHeaderData))]
    public async Task XForwardedHostAllowsValidCharacters(string hostHeader)
    {
        var assertsExecuted = false;

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedHost
                    });
                    app.Run(context =>
                    {
                        Assert.Equal(hostHeader, context.Request.Host.ToString());
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Host"] = hostHeader;
        });
        Assert.True(assertsExecuted);
    }

    public static TheoryData<string> HostHeaderInvalidData
    {
        get
        {
            // see https://tools.ietf.org/html/rfc7230#section-5.4
            var data = new TheoryData<string>() {
                    "", // Empty
                    "[]", // Too short
                    "[::]", // Too short
                    "[ghijkl]", // Non-hex
                    "[afd:adf:123", // Incomplete
                    "[afd:adf]123", // Missing :
                    "[afd:adf]:", // Missing port digits
                    "[afd adf]", // Space
                    "[ad-314]", // dash
                    ":1234", // Missing host
                    "a:b:c", // Missing []
                    "::1", // Missing []
                    "::", // Missing everything
                    "abcd:1abcd", // Letters in port
                    "abcd:1.2", // Dot in port
                    "1.2.3.4:", // Missing port digits
                    "1.2 .4", // Space
                };

            // These aren't allowed anywhere in the host header
            var invalid = "\"#%*+/;<=>?@[]\\^`{}|";
            foreach (var ch in invalid)
            {
                data.Add(ch.ToString());
            }

            invalid = "!\"#$%&'()*+,/;<=>?@[]\\^_`{}|~-";
            foreach (var ch in invalid)
            {
                data.Add("[abd" + ch + "]:1234");
            }

            invalid = "!\"#$%&'()*+/;<=>?@[]\\^_`{}|~:abcABC-.";
            foreach (var ch in invalid)
            {
                data.Add("a.b.c:" + ch);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(HostHeaderInvalidData))]
    public async Task XForwardedHostFailsForInvalidCharacters(string hostHeader)
    {
        var assertsExecuted = false;

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedHost
                    });
                    app.Run(context =>
                    {
                        Assert.NotEqual(hostHeader, context.Request.Host.Value);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Host"] = hostHeader;
        });
        Assert.True(assertsExecuted);
    }

    [Theory]
    [InlineData("localHost", "localhost")]
    [InlineData("localHost", "*")] // Any - Used by HttpSys
    [InlineData("localHost", "[::]")] // IPv6 Any - This is what Kestrel reports when binding to *
    [InlineData("localHost", "0.0.0.0")] // IPv4 Any
    [InlineData("localhost:9090", "example.com;localHost")]
    [InlineData("example.com:443", "example.com;localhost")]
    [InlineData("localHost:80", "localhost;")]
    [InlineData("foo.eXample.com:443", "*.exampLe.com")]
    [InlineData("f.eXample.com:443", "*.exampLe.com")]
    [InlineData("127.0.0.1", "127.0.0.1")]
    [InlineData("127.0.0.1:443", "127.0.0.1")]
    [InlineData("xn--c1yn36f:443", "xn--c1yn36f")]
    [InlineData("xn--c1yn36f:443", "點看")]
    [InlineData("[::ABC]", "[::aBc]")]
    [InlineData("[::1]:80", "[::1]")]
    public async Task XForwardedHostAllowsSpecifiedHost(string hostHeader, string allowedHost)
    {
        bool assertsExecuted = false;
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedHost,
                        AllowedHosts = allowedHost.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    });
                    app.Run(context =>
                    {
                        Assert.Equal(hostHeader, context.Request.Headers.Host);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var response = await server.SendAsync(ctx =>
        {
            ctx.Request.Headers["X-forwarded-Host"] = hostHeader;
        });
        Assert.True(assertsExecuted);
    }

    [Theory]
    [InlineData("example.com", "localhost")]
    [InlineData("localhost:9090", "example.com;")]
    [InlineData(";", "example.com;localhost")]
    [InlineData(";:80", "example.com;localhost")]
    [InlineData(":80", "localhost")]
    [InlineData(":", "localhost")]
    [InlineData("example.com:443", "*.example.com")]
    [InlineData(".example.com:443", "*.example.com")]
    [InlineData("foo.com:443", "*.example.com")]
    [InlineData("foo.example.com.bar:443", "*.example.com")]
    [InlineData(".com:443", "*.com")]
    // Unicode in the host shouldn't be allowed without punycode anyways. This match fails because the middleware converts
    // its input to punycode.
    [InlineData("點看", "點看")]
    [InlineData("[::1", "[::1]")]
    [InlineData("[::1:80", "[::1]")]
    public async Task XForwardedHostFailsMismatchedHosts(string hostHeader, string allowedHost)
    {
        bool assertsExecuted = false;
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedHost,
                        AllowedHosts = new[] { allowedHost }
                    });
                    app.Run(context =>
                    {
                        Assert.NotEqual<string>(hostHeader, context.Request.Headers.Host);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var response = await server.SendAsync(ctx =>
        {
            ctx.Request.Headers["X-forwarded-Host"] = hostHeader;
        });
        Assert.True(assertsExecuted);
    }

    [Fact]
    public async Task XForwardedHostStopsAtFirstUnspecifiedHost()
    {
        bool assertsExecuted = false;
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedHost,
                        ForwardLimit = 10,
                        AllowedHosts = new[] { "bar.com", "*.foo.com" }
                    });
                    app.Run(context =>
                    {
                        Assert.Equal("bar.foo.com:432", context.Request.Headers.Host);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var response = await server.SendAsync(ctx =>
        {
            ctx.Request.Headers["X-forwarded-Host"] = "stuff:523, bar.foo.com:432, bar.com:80";
        });
        Assert.True(assertsExecuted);
    }

    [Theory]
    [InlineData(0, "h1", "http")]
    [InlineData(1, "", "http")]
    [InlineData(1, "h1", "h1")]
    [InlineData(3, "h1", "h1")]
    [InlineData(1, "h2, h1", "h1")]
    [InlineData(2, "h2, h1", "h2")]
    [InlineData(10, "h3, h2, h1", "h3")]
    public async Task XForwardedProtoOverrideChangesRequestProtocol(int limit, string header, string expected)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedProto,
                        ForwardLimit = limit,
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Proto"] = header;
        });

        Assert.Equal(expected, context.Request.Scheme);
    }

    public static TheoryData<string> ProtoHeaderData
    {
        get
        {
            // ALPHA *( ALPHA / DIGIT / "+" / "-" / "." )
            return new TheoryData<string>() {
                    "z",
                    "Z",
                    "1",
                    "y+",
                    "1-",
                    "a.",
                };
        }
    }

    [Theory]
    [MemberData(nameof(ProtoHeaderData))]
    public async Task XForwardedProtoAcceptsValidProtocols(string scheme)
    {
        var assertsExecuted = false;

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedProto
                    });
                    app.Run(context =>
                    {
                        Assert.Equal(scheme, context.Request.Scheme);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Proto"] = scheme;
        });
        Assert.True(assertsExecuted);
    }

    public static TheoryData<string> ProtoHeaderInvalidData
    {
        get
        {
            // ALPHA *( ALPHA / DIGIT / "+" / "-" / "." )
            var data = new TheoryData<string>() {
                    "a b", // Space
                };

            // These aren't allowed anywhere in the scheme header
            var invalid = "!\"#$%&'()*/:;<=>?@[]\\^_`{}|~";
            foreach (var ch in invalid)
            {
                data.Add(ch.ToString());
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(ProtoHeaderInvalidData))]
    public async Task XForwardedProtoRejectsInvalidProtocols(string scheme)
    {
        var assertsExecuted = false;

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedProto,
                    });
                    app.Run(context =>
                    {
                        Assert.Equal("http", context.Request.Scheme);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Proto"] = scheme;
        });
        Assert.True(assertsExecuted);
    }

    [Theory]
    [InlineData(0, "h1", "::1", "http")]
    [InlineData(1, "", "::1", "http")]
    [InlineData(1, "h1", "::1", "h1")]
    [InlineData(3, "h1", "::1", "h1")]
    [InlineData(3, "h2, h1", "::1", "http")]
    [InlineData(5, "h2, h1", "::1, ::1", "h2")]
    [InlineData(10, "h3, h2, h1", "::1, ::1, ::1", "h3")]
    [InlineData(10, "h3, h2, h1", "::1, badip, ::1", "h1")]
    public async Task XForwardedProtoOverrideLimitedByXForwardedForCount(int limit, string protoHeader, string forHeader, string expected)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
                        RequireHeaderSymmetry = true,
                        ForwardLimit = limit,
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Proto"] = protoHeader;
            c.Request.Headers["X-Forwarded-For"] = forHeader;
        });

        Assert.Equal(expected, context.Request.Scheme);
    }

    [Theory]
    [InlineData(0, "h1", "::1", "http")]
    [InlineData(1, "", "::1", "http")]
    [InlineData(1, "h1", "", "h1")]
    [InlineData(1, "h1", "::1", "h1")]
    [InlineData(3, "h1", "::1", "h1")]
    [InlineData(3, "h1", "::1, ::1", "h1")]
    [InlineData(3, "h2, h1", "::1", "h2")]
    [InlineData(5, "h2, h1", "::1, ::1", "h2")]
    [InlineData(10, "h3, h2, h1", "::1, ::1, ::1", "h3")]
    [InlineData(10, "h3, h2, h1", "::1, badip, ::1", "h1")]
    public async Task XForwardedProtoOverrideCanBeIndependentOfXForwardedForCount(int limit, string protoHeader, string forHeader, string expected)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
                        RequireHeaderSymmetry = false,
                        ForwardLimit = limit,
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Proto"] = protoHeader;
            c.Request.Headers["X-Forwarded-For"] = forHeader;
        });

        Assert.Equal(expected, context.Request.Scheme);
    }

    [Theory]
    [InlineData("", "", "::1", false, "http")]
    [InlineData("h1", "", "::1", false, "http")]
    [InlineData("h1", "F::", "::1", false, "h1")]
    [InlineData("h1", "F::", "E::", false, "h1")]
    [InlineData("", "", "::1", true, "http")]
    [InlineData("h1", "", "::1", true, "http")]
    [InlineData("h1", "F::", "::1", true, "h1")]
    [InlineData("h1", "", "F::", true, "http")]
    [InlineData("h1", "E::", "F::", true, "http")]
    [InlineData("h2, h1", "", "::1", true, "http")]
    [InlineData("h2, h1", "F::, D::", "::1", true, "h1")]
    [InlineData("h2, h1", "E::, D::", "F::", true, "http")]
    public async Task XForwardedProtoOverrideLimitedByLoopback(string protoHeader, string forHeader, string remoteIp, bool loopback, string expected)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    var options = new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
                        RequireHeaderSymmetry = true,
                        ForwardLimit = 5,
                    };
                    if (!loopback)
                    {
                        options.KnownNetworks.Clear();
                        options.KnownProxies.Clear();
                    }
                    app.UseForwardedHeaders(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Proto"] = protoHeader;
            c.Request.Headers["X-Forwarded-For"] = forHeader;
            c.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        });

        Assert.Equal(expected, context.Request.Scheme);
    }

    [Fact]
    public void AllForwardsDisabledByDefault()
    {
        var options = new ForwardedHeadersOptions();
        Assert.True(options.ForwardedHeaders == ForwardedHeaders.None);
        Assert.Equal(1, options.ForwardLimit);
        Assert.Single(options.KnownNetworks);
        Assert.Single(options.KnownProxies);
    }

    [Fact]
    public async Task AllForwardsEnabledChangeRequestRemoteIpHostProtocolAndPathBase()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.All
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Proto"] = "Protocol";
            c.Request.Headers["X-Forwarded-For"] = "11.111.111.11";
            c.Request.Headers["X-Forwarded-Host"] = "testhost";
            c.Request.Headers["X-Forwarded-Prefix"] = "/pathbase";
        });

        Assert.Equal("11.111.111.11", context.Connection.RemoteIpAddress.ToString());
        Assert.Equal("testhost", context.Request.Host.ToString());
        Assert.Equal("Protocol", context.Request.Scheme);
        Assert.Equal("/pathbase", context.Request.PathBase);
    }

    [Fact]
    public async Task AllOptionsDisabledRequestDoesntChange()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.None
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Proto"] = "Protocol";
            c.Request.Headers["X-Forwarded-For"] = "11.111.111.11";
            c.Request.Headers["X-Forwarded-Host"] = "otherhost";
            c.Request.Headers["X-Forwarded-Prefix"] = "/pathbase";
        });

        Assert.Null(context.Connection.RemoteIpAddress);
        Assert.Equal("localhost", context.Request.Host.ToString());
        Assert.Equal("http", context.Request.Scheme);
        Assert.Equal(PathString.Empty, context.Request.PathBase);
    }

    [Fact]
    public async Task PartiallyEnabledForwardsPartiallyChangesRequest()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Proto"] = "Protocol";
            c.Request.Headers["X-Forwarded-For"] = "11.111.111.11";
        });

        Assert.Equal("11.111.111.11", context.Connection.RemoteIpAddress.ToString());
        Assert.Equal("localhost", context.Request.Host.ToString());
        Assert.Equal("Protocol", context.Request.Scheme);
    }

    [Theory]
    [InlineData("22.33.44.55,::ffff:127.0.0.1", "", "", "22.33.44.55")]
    [InlineData("22.33.44.55,::ffff:172.123.142.121", "172.123.142.121", "", "22.33.44.55")]
    [InlineData("22.33.44.55,::ffff:172.123.142.121", "::ffff:172.123.142.121", "", "22.33.44.55")]
    [InlineData("22.33.44.55,::ffff:172.123.142.121,172.32.24.23", "", "172.0.0.0/8", "22.33.44.55")]
    [InlineData("2a00:1450:4009:802::200e,2a02:26f0:2d:183::356e,::ffff:172.123.142.121,172.32.24.23", "", "172.0.0.0/8,2a02:26f0:2d:183::1/64", "2a00:1450:4009:802::200e")]
    [InlineData("22.33.44.55,2a02:26f0:2d:183::356e,::ffff:127.0.0.1", "2a02:26f0:2d:183::356e", "", "22.33.44.55")]
    public async Task XForwardForIPv4ToIPv6Mapping(string forHeader, string knownProxies, string knownNetworks, string expectedRemoteIp)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor,
            ForwardLimit = null,
        };

        foreach (var knownProxy in knownProxies.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries))
        {
            var proxy = IPAddress.Parse(knownProxy);
            options.KnownProxies.Add(proxy);
        }
        foreach (var knownNetwork in knownNetworks.Split(new string[] { "," }, options: StringSplitOptions.RemoveEmptyEntries))
        {
            var knownNetworkParts = knownNetwork.Split('/');
            var networkIp = IPAddress.Parse(knownNetworkParts[0]);
            var prefixLength = int.Parse(knownNetworkParts[1], CultureInfo.InvariantCulture);
            options.KnownNetworks.Add(new IPNetwork(networkIp, prefixLength));
        }

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-For"] = forHeader;
        });

        Assert.Equal(expectedRemoteIp, context.Connection.RemoteIpAddress.ToString());
    }

    [Theory]
    [InlineData(1, "httpa, httpb, httpc", "httpc", "httpa,httpb")]
    [InlineData(2, "httpa, httpb, httpc", "httpb", "httpa")]
    public async Task ForwardersWithDIOptionsRunsOnce(int limit, string header, string expectedScheme, string remainingHeader)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.Configure<ForwardedHeadersOptions>(options =>
                    {
                        options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
                        options.KnownProxies.Clear();
                        options.KnownNetworks.Clear();
                        options.ForwardLimit = limit;
                    });
                })
                .Configure(app =>
                {
                    app.UseForwardedHeaders();
                    app.UseForwardedHeaders();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Proto"] = header;
        });

        Assert.Equal(expectedScheme, context.Request.Scheme);
        Assert.Equal(remainingHeader, context.Request.Headers["X-Forwarded-Proto"].ToString());
    }

    [Theory]
    [InlineData(1, "httpa, httpb, httpc", "httpb", "httpa")]
    [InlineData(2, "httpa, httpb, httpc", "httpa", "")]
    public async Task ForwardersWithDirectOptionsRunsTwice(int limit, string header, string expectedScheme, string remainingHeader)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    var options = new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedProto,
                        ForwardLimit = limit,
                    };
                    options.KnownProxies.Clear();
                    options.KnownNetworks.Clear();
                    app.UseForwardedHeaders(options);
                    app.UseForwardedHeaders(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Proto"] = header;
        });

        Assert.Equal(expectedScheme, context.Request.Scheme);
        Assert.Equal(remainingHeader, context.Request.Headers["X-Forwarded-Proto"].ToString());
    }

    [Theory]
    [InlineData("/foo", "/foo")]
    [InlineData("/foo/", "/foo/")]
    [InlineData("/foo/bar", "/foo/bar")]
    [InlineData("/foo%20bar", "/foo bar")]
    [InlineData("/foo?bar?", "/foo?bar?")]
    [InlineData("/foo%2F", "/foo%2F")]
    [InlineData("/foo%2F/", "/foo%2F/")]
    public async Task XForwardedPrefixReplaceEmptyPathBase(
        string forwardedPrefix,
        string expectedUnescapedPathBase)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedPrefix,
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Prefix"] = forwardedPrefix;
        });

        Assert.Equal(expectedUnescapedPathBase, context.Request.PathBase.Value);
        // No X-Original-Prefix header set when original PathBase is empty
        Assert.False(context.Request.Headers.ContainsKey("X-Original-Prefix"));
        // Should have been consumed and removed
        Assert.False(context.Request.Headers.ContainsKey("X-Forwarded-Prefix"));
    }

    [Theory]
    [InlineData("/foo", "/bar", "/bar", "/foo")]
    [InlineData("/foo", "/", "/", "/foo")]
    [InlineData("/foo", "/foo/bar", "/foo/bar", "/foo")]
    [InlineData("/foo/bar", "/foo", "/foo", "/foo/bar")]
    [InlineData("/foo bar", "/", "/", "/foo%20bar")]
    public async Task XForwardedPrefixReplaceNonEmptyPathBase(
        string pathBase,
        string forwardedPrefix,
        string expectedUnescapedPathBase,
        string expectedOriginalPrefix)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedPrefix,
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.PathBase = new PathString(pathBase);
            c.Request.Headers["X-Forwarded-Prefix"] = forwardedPrefix;
        });

        Assert.Equal(expectedUnescapedPathBase, context.Request.PathBase.Value);
        Assert.Equal(expectedOriginalPrefix, context.Request.Headers["X-Original-Prefix"]);
        // Should have been consumed and removed
        Assert.False(context.Request.Headers.ContainsKey("X-Forwarded-Prefix"));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid/")]
    [InlineData("%2Finvalid")]
    public async Task XForwardedPrefixInvalidPath(string forwardedPrefix)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedPrefix,
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Prefix"] = forwardedPrefix;
        });

        Assert.Equal(PathString.Empty, context.Request.PathBase);
        Assert.False(context.Request.Headers.ContainsKey("X-Original-Prefix"));
        Assert.True(context.Request.Headers.ContainsKey("X-Forwarded-Prefix"));
    }

    [Theory]
    [InlineData("11.111.111.11", "host1, host2", "h1, h2", "/prefix1, /prefix2")]
    [InlineData("11.111.111.11, 22.222.222.22", "host1", "h1, h2", "/prefix1, /prefix2")]
    [InlineData("11.111.111.11, 22.222.222.22", "host1, host2", "h1", "/prefix1, /prefix2")]
    [InlineData("11.111.111.11, 22.222.222.22", "host1, host2", "h1, h2", "/prefix1")]
    public async Task XForwardedPrefixParameterCountMismatch(
        string forwardedFor,
        string forwardedHost,
        string forwardedProto,
        string forwardedPrefix)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders =
                            ForwardedHeaders.XForwardedFor |
                            ForwardedHeaders.XForwardedHost |
                            ForwardedHeaders.XForwardedProto |
                            ForwardedHeaders.XForwardedPrefix,
                        RequireHeaderSymmetry = true,
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-For"] = forwardedFor;
            c.Request.Headers["X-Forwarded-Host"] = forwardedHost;
            c.Request.Headers["X-Forwarded-Proto"] = forwardedProto;
            c.Request.Headers["X-Forwarded-Prefix"] = forwardedPrefix;
        });

        Assert.Equal(PathString.Empty, context.Request.PathBase);
        Assert.False(context.Request.Headers.ContainsKey("X-Original-For"));
        Assert.False(context.Request.Headers.ContainsKey("X-Original-Host"));
        Assert.False(context.Request.Headers.ContainsKey("X-Original-Proto"));
        Assert.False(context.Request.Headers.ContainsKey("X-Original-Prefix"));
        Assert.True(context.Request.Headers.ContainsKey("X-Forwarded-For"));
        Assert.True(context.Request.Headers.ContainsKey("X-Forwarded-Host"));
        Assert.True(context.Request.Headers.ContainsKey("X-Forwarded-Proto"));
        Assert.True(context.Request.Headers.ContainsKey("X-Forwarded-Prefix"));
    }

    [Theory]
    [InlineData(1, "/prefix1, /prefix2", "/prefix1")]
    [InlineData(1, "/prefix1, /prefix2, /prefix3", "/prefix1,/prefix2")]
    public async Task XForwardedPrefixTruncateConsumedValues(
        int limit,
        string forwardedPrefix,
        string expectedforwardedPrefix)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedPrefix,
                        ForwardLimit = limit,
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Prefix"] = forwardedPrefix;
        });

        Assert.Equal(expectedforwardedPrefix, context.Request.Headers["X-Forwarded-Prefix"].ToString());
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Rewrite.Tests.CodeRules;

public class MiddlewareTests
{
    [Fact]
    public async Task CheckRewritePath()
    {
        var options = new RewriteOptions().AddRewrite("(.*)", "http://example.com/$1", skipRemainingRules: false);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(
                        context.Request.Scheme +
                        "://" +
                        context.Request.Host +
                        context.Request.Path +
                        context.Request.QueryString));
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetStringAsync("foo");

        Assert.Equal("http://example.com/foo", response);
    }

    [Fact]
    public async Task CheckRewritePathWithSkipRemaining()
    {
        var options = new RewriteOptions().AddRewrite("(.*)", "http://example.com/$1", skipRemainingRules: true);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(
                        context.Request.Scheme +
                        "://" +
                        context.Request.Host +
                        context.Request.Path +
                        context.Request.QueryString));
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetStringAsync("foo");

        Assert.Equal("http://example.com/foo", response);
    }

    [Fact]
    public async Task CheckRewritePath_MultipleRulesWithSkipRemaining()
    {
        var options = new RewriteOptions()
            .AddRewrite("(.*)", "http://example.com/$1", skipRemainingRules: true)
            .AddRewrite("(.*)", "http://example.com/42", skipRemainingRules: false);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(
                        context.Request.Scheme +
                        "://" +
                        context.Request.Host +
                        context.Request.Path +
                        context.Request.QueryString));
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetStringAsync("foo");

        Assert.Equal("http://example.com/foo", response);
    }

    [Fact]
    public async Task CheckRewritePath_MultipleRules()
    {
        var options = new RewriteOptions()
            .AddRewrite("(.*)", "http://example.com/$1s", skipRemainingRules: false)
            .AddRewrite("(.*)", "http://example.com/$1/42", skipRemainingRules: false);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(
                        context.Request.Scheme +
                        "://" +
                        context.Request.Host +
                        context.Request.Path +
                        context.Request.QueryString));
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetStringAsync("foo");

        Assert.Equal("http://example.com/foos/42", response);
    }

    [Theory]
    [InlineData("(.*)", "http://example.com/$1", null, "path", "http://example.com/path")]
    [InlineData("(.*)", "http://example.com", null, "", "http://example.com/")]
    [InlineData("(z*)", "$1", null, "path", "/")]
    [InlineData("(z*)", "http://example.com/$1", null, "path", "http://example.com/")]
    [InlineData("(z*)", "$1", "http://example.com/pathBase", "/pathBase/path", "/pathBase")]
    [InlineData("path/(.*)", "path?value=$1", null, "path/value", "/path?value=value")]
    [InlineData("path/(.*)", "path?param=$1", null, "path/value?param1=OtherValue", "/path?param1=OtherValue&param=value")]
    [InlineData("path/(.*)", "http://example.com/pathBase/path?param=$1", "http://example.com/pathBase", "path/value?param1=OtherValue", "http://example.com/pathBase/path?param1=OtherValue&param=value")]
    [InlineData("path/(.*)", "http://hoψst.com/pÂthBase/path?parãm=$1", "http://example.com/pathBase", "path/value?päram1=OtherValüe", "http://xn--host-cpd.com/p%C3%82thBase/path?p%C3%A4ram1=OtherVal%C3%BCe&parãm=value")]
    public async Task CheckRedirectPath(string pattern, string replacement, string baseAddress, string requestUrl, string expectedUrl)
    {
        var options = new RewriteOptions().AddRedirect(pattern, replacement, statusCode: StatusCodes.Status301MovedPermanently);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        if (!string.IsNullOrEmpty(baseAddress))
        {
            server.BaseAddress = new Uri(baseAddress);
        }

        var response = await server.CreateClient().GetAsync(requestUrl);

        Assert.Equal(expectedUrl, response.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task RewriteRulesCanComeFromConfigureOptions()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.Configure<RewriteOptions>(options =>
                    {
                        options.AddRedirect("(.*)", "http://example.com/$1", statusCode: StatusCodes.Status301MovedPermanently);
                    });
                })
                .Configure(app =>
                {
                    app.UseRewriter();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync("foo");

        Assert.Equal("http://example.com/foo", response.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task CheckRedirectPathWithQueryString()
    {
        var options = new RewriteOptions().AddRedirect("(.*)", "http://example.com/$1", statusCode: StatusCodes.Status301MovedPermanently);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync("foo?bar=1");

        Assert.Equal("http://example.com/foo?bar=1", response.Headers.Location.OriginalString);
    }

    [Theory]
    [InlineData(StatusCodes.Status301MovedPermanently)]
    [InlineData(StatusCodes.Status302Found)]
    [InlineData(StatusCodes.Status307TemporaryRedirect)]
    [InlineData(StatusCodes.Status308PermanentRedirect)]
    public async Task CheckRedirectToHttpsStatus(int statusCode)
    {
        var options = new RewriteOptions().AddRedirectToHttps(statusCode: statusCode);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync(new Uri("http://example.com"));

        Assert.Equal("https://example.com/", response.Headers.Location.OriginalString);
        Assert.Equal(statusCode, (int)response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(123)]
    public async Task CheckRedirectToHttpsSslPort(int? sslPort)
    {
        var options = new RewriteOptions().AddRedirectToHttps(statusCode: StatusCodes.Status302Found, sslPort: sslPort);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync(new Uri("http://example.com"));

        if (sslPort.HasValue)
        {
            Assert.Equal($"https://example.com:{sslPort.GetValueOrDefault().ToString(CultureInfo.InvariantCulture)}/", response.Headers.Location.OriginalString);
        }
        else
        {
            Assert.Equal("https://example.com/", response.Headers.Location.OriginalString);
        }
    }

    [Theory]
    [InlineData(null, "example.com", "example.com/")]
    [InlineData(null, "example.com/path", "example.com/path")]
    [InlineData(null, "example.com/path?name=value", "example.com/path?name=value")]
    [InlineData(null, "hoψst.com", "xn--host-cpd.com/")]
    [InlineData(null, "hoψst.com/path", "xn--host-cpd.com/path")]
    [InlineData(null, "hoψst.com/path?name=value", "xn--host-cpd.com/path?name=value")]
    [InlineData(null, "example.com/pãth", "example.com/p%C3%A3th")]
    [InlineData(null, "example.com/path?näme=valüe", "example.com/path?n%C3%A4me=val%C3%BCe")]
    [InlineData("example.com/pathBase", "example.com/pathBase/path", "example.com/pathBase/path")]
    [InlineData("example.com/pathBase", "example.com/pathBase", "example.com/pathBase")]
    [InlineData("example.com/pâthBase", "example.com/pâthBase/path", "example.com/p%C3%A2thBase/path")]
    public async Task CheckRedirectToHttpsUrl(string baseAddress, string hostPathAndQuery, string expectedHostPathAndQuery)
    {
        var options = new RewriteOptions().AddRedirectToHttps();
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        if (!string.IsNullOrEmpty(baseAddress))
        {
            server.BaseAddress = new Uri("http://" + baseAddress);
        }

        var response = await server.CreateClient().GetAsync(new Uri("http://" + hostPathAndQuery));

        Assert.Equal("https://" + expectedHostPathAndQuery, response.Headers.Location.OriginalString);
    }

    [Theory]
    [InlineData("http://example.com/test", "http://www.example.com/")]
    [InlineData("http://example.com/test", "https://www.example.com/")]
    [InlineData("https://example.com/test", "http://www.example.com/")]
    [InlineData("https://example.com/test", "https://www.example.com/")]
    public async Task CheckRedirectUsesConfiguredScheme(string hostSchemePathAndQuery, string redirectReplacement)
    {
        var options = new RewriteOptions().AddRedirect("test", redirectReplacement);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        server.BaseAddress = new Uri("http://example.com");
        var response = await server.CreateClient().GetAsync(new Uri(hostSchemePathAndQuery));

        // Regardless of whether we GET with http or https, the redirect should honor
        // the scheme specified in the configuration.
        Assert.Equal(redirectReplacement, response.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task CheckPermanentRedirectToHttps()
    {
        var options = new RewriteOptions().AddRedirectToHttpsPermanent();
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync(new Uri("http://example.com"));

        Assert.Equal("https://example.com/", response.Headers.Location.OriginalString);
        Assert.Equal(StatusCodes.Status301MovedPermanently, (int)response.StatusCode);
    }

    [Theory]
    [InlineData(25, "https://example.com:25/")]
    [InlineData(-25, "https://example.com/")]
    public async Task CheckRedirectToHttpsWithSslPort(int sslPort, string expected)
    {
        var options = new RewriteOptions().AddRedirectToHttps(statusCode: StatusCodes.Status301MovedPermanently, sslPort: sslPort);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync(new Uri("http://example.com"));

        Assert.Equal(expected, response.Headers.Location.OriginalString);
        Assert.Equal(StatusCodes.Status301MovedPermanently, (int)response.StatusCode);
    }

    [Theory]
    [InlineData(StatusCodes.Status301MovedPermanently)]
    [InlineData(StatusCodes.Status302Found)]
    [InlineData(StatusCodes.Status307TemporaryRedirect)]
    [InlineData(StatusCodes.Status308PermanentRedirect)]
    public async Task CheckRedirectToWwwWithStatusCode(int statusCode)
    {
        var options = new RewriteOptions().AddRedirectToWww(statusCode: statusCode);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync(new Uri("https://example.com"));

        Assert.Equal("https://www.example.com/", response.Headers.Location.OriginalString);
        Assert.Equal(statusCode, (int)response.StatusCode);
    }

    [Theory]
    [InlineData("http://example.com", "http://www.example.com/")]
    [InlineData("https://example.com", "https://www.example.com/")]
    [InlineData("http://example.com:8081", "http://www.example.com:8081/")]
    [InlineData("http://example.com:8081/example?q=1", "http://www.example.com:8081/example?q=1")]
    public async Task CheckRedirectToWww(string requestUri, string redirectUri)
    {
        var options = new RewriteOptions().AddRedirectToWww();
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync(new Uri(requestUri));

        Assert.Equal(redirectUri, response.Headers.Location.OriginalString);
        Assert.Equal(StatusCodes.Status307TemporaryRedirect, (int)response.StatusCode);
    }

    [Fact]
    public async Task CheckPermanentRedirectToWww()
    {
        var options = new RewriteOptions().AddRedirectToWwwPermanent();
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync(new Uri("https://example.com"));

        Assert.Equal("https://www.example.com/", response.Headers.Location.OriginalString);
        Assert.Equal(StatusCodes.Status308PermanentRedirect, (int)response.StatusCode);
    }

    [Theory]
    [InlineData("http://www.example.com")]
    [InlineData("https://www.example.com")]
    [InlineData("http://www.example.com:8081")]
    [InlineData("https://www.example.com:8081")]
    [InlineData("https://www.example.com:8081/example?q=1")]
    [InlineData("http://localhost")]
    [InlineData("https://localhost")]
    [InlineData("http://localhost:8081")]
    [InlineData("https://localhost:8081")]
    [InlineData("https://localhost:8081/example?q=1")]
    public async Task CheckNoRedirectToWww(string requestUri)
    {
        var options = new RewriteOptions().AddRedirectToWww();
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync(new Uri(requestUri));

        Assert.Null(response.Headers.Location);
    }

    [Theory]
    [InlineData(StatusCodes.Status301MovedPermanently)]
    [InlineData(StatusCodes.Status302Found)]
    [InlineData(StatusCodes.Status307TemporaryRedirect)]
    [InlineData(StatusCodes.Status308PermanentRedirect)]
    public async Task CheckRedirectToNonWwwWithStatusCode(int statusCode)
    {
        var options = new RewriteOptions().AddRedirectToNonWww(statusCode: statusCode);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync(new Uri("https://www.example.com"));

        Assert.Equal("https://example.com/", response.Headers.Location.OriginalString);
        Assert.Equal(statusCode, (int)response.StatusCode);
    }

    [Theory]
    [InlineData("http://www.example.com", "http://example.com/")]
    [InlineData("https://www.example.com", "https://example.com/")]
    [InlineData("http://www.example.com:8081", "http://example.com:8081/")]
    [InlineData("http://www.example.com:8081/example?q=1", "http://example.com:8081/example?q=1")]
    public async Task CheckRedirectToNonWww(string requestUri, string redirectUri)
    {
        var options = new RewriteOptions().AddRedirectToNonWww();
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync(new Uri(requestUri));

        Assert.Equal(redirectUri, response.Headers.Location.OriginalString);
        Assert.Equal(StatusCodes.Status307TemporaryRedirect, (int)response.StatusCode);
    }

    [Fact]
    public async Task CheckPermanentRedirectToNonWww()
    {
        var options = new RewriteOptions().AddRedirectToNonWwwPermanent();
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync(new Uri("https://www.example.com"));

        Assert.Equal("https://example.com/", response.Headers.Location.OriginalString);
        Assert.Equal(StatusCodes.Status308PermanentRedirect, (int)response.StatusCode);
    }

    [Fact]
    public async Task CheckIfEmptyStringRedirectCorrectly()
    {
        var options = new RewriteOptions().AddRedirect("(.*)", "$1", statusCode: StatusCodes.Status301MovedPermanently);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync("");
        Assert.Equal("/", response.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task RewriteAfterUseRoutingHitsOriginalEndpoint()
    {
        // This is an edge case where users setup routing incorrectly, but we don't want to accidentally change behavior in case someone
        // relies on it, so we have this test
        var options = new RewriteOptions().AddRewrite("(.*)", "$1s", skipRemainingRules: false);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(s =>
                {
                    s.AddRouting();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseRewriter(options);

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/foos", context => context.Response.WriteAsync("bad"));
                        endpoints.MapGet("/foo", context => context.Response.WriteAsync($"{context.GetEndpoint()?.DisplayName} from {context.Request.Path}"));
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetStringAsync("foo");

        Assert.Equal("HTTP: GET /foo from /foos", response);
    }

    [Fact]
    public async Task CheckIfEmptyStringRewriteCorrectly()
    {
        var options = new RewriteOptions().AddRewrite("(.*)", "$1", skipRemainingRules: false);
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(
                            context.Request.Path +
                            context.Request.QueryString));
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetStringAsync("");

        Assert.Equal("/", response);
    }

    [Fact]
    public async Task SettingPathBase()
    {
        var options = new RewriteOptions().AddRedirect("(.*)", "$1");
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(
                            context.Request.Path +
                            context.Request.QueryString));
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:5000/foo");

        var response = await server.CreateClient().GetAsync("");

        Assert.Equal("/foo", response.Headers.Location.OriginalString);
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com")]
    [InlineData("http://example.com:8081")]
    [InlineData("https://example.com:8081")]
    [InlineData("https://example.com:8081/example?q=1")]
    public async Task CheckNoRedirectToWwwInNonWhitelistedDomains(string requestUri)
    {
        var options = new RewriteOptions().AddRedirectToWww("example2.com");
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync(new Uri(requestUri));

        Assert.Null(response.Headers.Location);
    }

    [Theory]
    [InlineData("http://example.com/", "http://www.example.com/")]
    [InlineData("https://example.com/", "https://www.example.com/")]
    [InlineData("http://example.com:8081", "http://www.example.com:8081/")]
    [InlineData("http://example.com:8081/example?q=1", "http://www.example.com:8081/example?q=1")]
    public async Task CheckRedirectToWwwInWhitelistedDomains(string requestUri, string redirectUri)
    {
        var options = new RewriteOptions().AddRedirectToWww("example.com");
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync(new Uri(requestUri));

        Assert.Equal(redirectUri, response.Headers.Location.OriginalString);
        Assert.Equal(StatusCodes.Status307TemporaryRedirect, (int)response.StatusCode);
    }

    [Fact]
    public async Task CheckPermanentRedirectToWwwInWhitelistedDomains()
    {
        var options = new RewriteOptions().AddRedirectToWwwPermanent("example.com");
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync(new Uri("https://example.com"));

        Assert.Equal("https://www.example.com/", response.Headers.Location.OriginalString);
        Assert.Equal(StatusCodes.Status308PermanentRedirect, (int)response.StatusCode);
    }

    [Theory]
    [InlineData(StatusCodes.Status301MovedPermanently)]
    [InlineData(StatusCodes.Status302Found)]
    [InlineData(StatusCodes.Status307TemporaryRedirect)]
    [InlineData(StatusCodes.Status308PermanentRedirect)]
    public async Task CheckRedirectToWwwWithStatusCodeInWhitelistedDomains(int statusCode)
    {
        var options = new RewriteOptions().AddRedirectToWww(statusCode: statusCode, "example.com");
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync(new Uri("https://example.com"));

        Assert.Equal("https://www.example.com/", response.Headers.Location.OriginalString);
        Assert.Equal(statusCode, (int)response.StatusCode);
    }

    [Theory]
    [InlineData("(.*)", "http://example.com/g")]
    [InlineData("/", "no rule")]
    public async Task Rewrite_WorksAfterUseRoutingIfGlobalRouteBuilderUsed(string regex, string output)
    {
        var options = new RewriteOptions().AddRewrite(regex, "http://example.com/g", skipRemainingRules: false);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.UseRouting();

        app.UseRewriter(options);

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/foo", context => context.Response.WriteAsync(
                "no rule"));

            endpoints.MapGet("/g", context => context.Response.WriteAsync(
                context.Request.Scheme +
                "://" +
                context.Request.Host +
                context.Request.Path +
                context.Request.QueryString));
        });

        await app.StartAsync();

        var server = app.GetTestServer();

        var response = await server.CreateClient().GetStringAsync("foo");

        Assert.Equal(output, response);
    }

    [Theory]
    [InlineData("(.*)", "http://example.com/g")]
    [InlineData("/", "no rule")]
    public async Task RewriteFromOptions_WorksAfterUseRoutingIfGlobalRouteBuilderUsed(string regex, string output)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.Configure<RewriteOptions>(options =>
        {
            options.AddRewrite(regex, "http://example.com/g", skipRemainingRules: false);
        });
        await using var app = builder.Build();

        app.UseRouting();
        app.UseRewriter();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/foo", context => context.Response.WriteAsync(
                "no rule"));

            endpoints.MapGet("/g", context => context.Response.WriteAsync(
                context.Request.Scheme +
                "://" +
                context.Request.Host +
                context.Request.Path +
                context.Request.QueryString));
        });

        await app.StartAsync();

        var server = app.GetTestServer();

        var response = await server.CreateClient().GetStringAsync("foo");

        Assert.Equal(output, response);
    }

    [Fact]
    public async Task RewriteSkipRemaing_WorksAfterUseRoutingIfGlobalRouteBuilderUsed()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.Configure<RewriteOptions>(options =>
        {
            options.AddRewrite("(.*)", "http://example.com/g", skipRemainingRules: true);
        });
        await using var app = builder.Build();
        app.UseRouting();

        app.UseRewriter();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/foo", context => context.Response.WriteAsync(
                "no rule"));

            endpoints.MapGet("/g", context => context.Response.WriteAsync(
                context.Request.Scheme +
                "://" +
                context.Request.Host +
                context.Request.Path +
                context.Request.QueryString));
        });

        await app.StartAsync();

        var server = app.GetTestServer();

        var response = await server.CreateClient().GetStringAsync("foo");

        Assert.Equal("http://example.com/g", response);
    }

    [Fact]
    public async Task RewriteWithMultipleRules_WorksAfterUseRoutingIfGlobalRouteBuilderUsed()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.Configure<RewriteOptions>(options =>
        {
            options.AddRewrite("(.*)", "http://example.com/g", skipRemainingRules: false)
                .AddRewrite("(.*)", "http://example.com/$1/h", skipRemainingRules: false);
        });
        await using var app = builder.Build();
        app.UseRouting();

        app.UseRewriter();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/foo", context => context.Response.WriteAsync(
                "no rule"));

            endpoints.MapGet("/g/h", context => context.Response.WriteAsync(
                context.Request.Scheme +
                "://" +
                context.Request.Host +
                context.Request.Path +
                context.Request.QueryString));
        });

        await app.StartAsync();

        var server = app.GetTestServer();

        var response = await server.CreateClient().GetStringAsync("foo");

        Assert.Equal("http://example.com/g/h", response);
    }

    [Fact]
    public async Task RewriteWithMultipleRulesAndSkip_WorksAfterUseRoutingIfGlobalRouteBuilderUsed()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.Configure<RewriteOptions>(options =>
        {
            options.AddRewrite("(.*)", "http://example.com/g", skipRemainingRules: true)
                .AddRewrite("(.*)", "http://example.com/$1/h", skipRemainingRules: false);
        });
        await using var app = builder.Build();
        app.UseRouting();

        app.UseRewriter();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/foo", context => context.Response.WriteAsync(
                "no rule"));

            endpoints.MapGet("/g", context => context.Response.WriteAsync(
                context.Request.Scheme +
                "://" +
                context.Request.Host +
                context.Request.Path +
                context.Request.QueryString));
        });

        await app.StartAsync();

        var server = app.GetTestServer();

        var response = await server.CreateClient().GetStringAsync("foo");

        Assert.Equal("http://example.com/g", response);
    }

    [Fact]
    public async Task Rewrite_WorksWithoutUseRoutingWithWebApplication()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.Configure<RewriteOptions>(options =>
        {
            options.AddRewrite("(.*)", "http://example.com/g", skipRemainingRules: true);
        });
        await using var app = builder.Build();

        app.UseRewriter();

        app.MapGet("/foo", context => context.Response.WriteAsync(
            "no rule"));

        app.MapGet("/g", context => context.Response.WriteAsync(
            context.Request.Scheme +
            "://" +
            context.Request.Host +
            context.Request.Path +
            context.Request.QueryString));

        await app.StartAsync();

        var server = app.GetTestServer();

        var response = await server.CreateClient().GetStringAsync("foo");

        Assert.Equal("http://example.com/g", response);
    }
}

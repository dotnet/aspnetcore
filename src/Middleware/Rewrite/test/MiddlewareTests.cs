// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.CodeRules
{
    public class MiddlewareTests
    {
        [Fact]
        public async Task CheckRewritePath()
        {
            var options = new RewriteOptions().AddRewrite("(.*)", "http://example.com/$1", skipRemainingRules: false);
            var builder = new WebHostBuilder()
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
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync("foo");

            Assert.Equal("http://example.com/foo", response);
        }

        [Fact]
        public async Task CheckRedirectPath()
        {
            var options = new RewriteOptions().AddRedirect("(.*)", "http://example.com/$1", statusCode: StatusCodes.Status301MovedPermanently);
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
            });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("foo");

            Assert.Equal("http://example.com/foo", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task RewriteRulesCanComeFromConfigureOptions()
        {
            var builder = new WebHostBuilder()
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
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("foo");

            Assert.Equal("http://example.com/foo", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task CheckRedirectPathWithQueryString()
        {
            var options = new RewriteOptions().AddRedirect("(.*)", "http://example.com/$1", statusCode: StatusCodes.Status301MovedPermanently);
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
            });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("foo?bar=1");

            Assert.Equal("http://example.com/foo?bar=1", response.Headers.Location.OriginalString);
        }

        [Theory]
        [InlineData(StatusCodes.Status301MovedPermanently)]
        [InlineData(StatusCodes.Status302Found)]
        [InlineData(StatusCodes.Status307TemporaryRedirect)]
        [InlineData(StatusCodes.Status308PermanentRedirect)]
        public async Task CheckRedirectToHttps(int statusCode)
        {
            var options = new RewriteOptions().AddRedirectToHttps(statusCode: statusCode);
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
            });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync(new Uri("http://example.com"));

            Assert.Equal("https://example.com/", response.Headers.Location.OriginalString);
            Assert.Equal(statusCode, (int)response.StatusCode);
        }

        [Fact]
        public async Task CheckPermanentRedirectToHttps()
        {
            var options = new RewriteOptions().AddRedirectToHttpsPermanent();
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            var server = new TestServer(builder);

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
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
            });
            var server = new TestServer(builder);

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
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
            });
            var server = new TestServer(builder);

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
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync(new Uri(requestUri));

            Assert.Equal(redirectUri, response.Headers.Location.OriginalString);
            Assert.Equal(StatusCodes.Status307TemporaryRedirect, (int)response.StatusCode);
        }

        [Fact]
        public async Task CheckPermanentRedirectToWww()
        {
            var options = new RewriteOptions().AddRedirectToWwwPermanent();
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            var server = new TestServer(builder);

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
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
            });
            var server = new TestServer(builder);

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
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
            });
            var server = new TestServer(builder);

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
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync(new Uri(requestUri));

            Assert.Equal(redirectUri, response.Headers.Location.OriginalString);
            Assert.Equal(StatusCodes.Status307TemporaryRedirect, (int)response.StatusCode);
        }

        [Fact]
        public async Task CheckPermanentRedirectToNonWww()
        {
            var options = new RewriteOptions().AddRedirectToNonWwwPermanent();
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync(new Uri("https://www.example.com"));

            Assert.Equal("https://example.com/", response.Headers.Location.OriginalString);
            Assert.Equal(StatusCodes.Status308PermanentRedirect, (int)response.StatusCode);
        }

        [Fact]
        public async Task CheckIfEmptyStringRedirectCorrectly()
        {
            var options = new RewriteOptions().AddRedirect("(.*)", "$1", statusCode: StatusCodes.Status301MovedPermanently);
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
            });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("");
            Assert.Equal("/", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task CheckIfEmptyStringRewriteCorrectly()
        {
            var options = new RewriteOptions().AddRewrite("(.*)", "$1", skipRemainingRules: false);
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
                app.Run(context => context.Response.WriteAsync(
                        context.Request.Path +
                        context.Request.QueryString));
            });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync("");

            Assert.Equal("/", response);
        }

        [Fact]
        public async Task SettingPathBase()
        {
            var options = new RewriteOptions().AddRedirect("(.*)", "$1");
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
                app.Run(context => context.Response.WriteAsync(
                        context.Request.Path +
                        context.Request.QueryString));
            });
            var server = new TestServer(builder) { BaseAddress = new Uri("http://localhost:5000/foo") };

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
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
            });
            var server = new TestServer(builder);

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
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync(new Uri(requestUri));

            Assert.Equal(redirectUri, response.Headers.Location.OriginalString);
            Assert.Equal(StatusCodes.Status307TemporaryRedirect, (int)response.StatusCode);
        }

        [Fact]
        public async Task CheckPermanentRedirectToWwwInWhitelistedDomains()
        {
            var options = new RewriteOptions().AddRedirectToWwwPermanent("example.com");
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            var server = new TestServer(builder);

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
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
            });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync(new Uri("https://example.com"));

            Assert.Equal("https://www.example.com/", response.Headers.Location.OriginalString);
            Assert.Equal(statusCode, (int)response.StatusCode);
        }

    }
}

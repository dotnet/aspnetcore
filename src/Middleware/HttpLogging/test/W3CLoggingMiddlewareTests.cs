// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.HttpLogging;

public class W3CLoggingMiddlewareTests
{
    [Fact]
    public void Ctor_ThrowsExceptionsWhenNullArgs()
    {
        var options = CreateOptionsAccessor();
        Assert.Throws<ArgumentNullException>(() => new W3CLoggingMiddleware(
            null,
            options,
            Helpers.CreateTestW3CLogger(options)));

        Assert.Throws<ArgumentNullException>(() => new W3CLoggingMiddleware(c =>
            {
                return Task.CompletedTask;
            },
            null,
            Helpers.CreateTestW3CLogger(options)));

        Assert.Throws<ArgumentNullException>(() => new W3CLoggingMiddleware(c =>
            {
                return Task.CompletedTask;
            },
            options,
            null));
    }

    [Fact]
    public async Task NoopWhenLoggingDisabled()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = W3CLoggingFields.None;
        var logger = Helpers.CreateTestW3CLogger(options);

        var middleware = new W3CLoggingMiddleware(
            c =>
            {
                c.Response.StatusCode = 200;
                return Task.CompletedTask;
            },
            options,
            logger);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Protocol = "HTTP/1.0";
        httpContext.Request.Method = "GET";
        httpContext.Request.Path = new PathString("/foo");
        httpContext.Request.QueryString = new QueryString("?foo");
        httpContext.Request.Headers["Referer"] = "bar";

        await middleware.Invoke(httpContext);

        Assert.Empty(logger.Processor.Lines);
    }

    [Fact]
    public async Task DefaultDoesNotLogOptionalFields()
    {
        var options = CreateOptionsAccessor();
        var logger = Helpers.CreateTestW3CLogger(options);

        var middleware = new W3CLoggingMiddleware(
            c =>
            {
                c.Response.StatusCode = 200;
                return Task.CompletedTask;
            },
            options,
            logger);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Protocol = "HTTP/1.0";
        httpContext.Request.Headers["Cookie"] = "Snickerdoodle";
        httpContext.Response.StatusCode = 200;

        var now = DateTime.UtcNow;
        await middleware.Invoke(httpContext);
        await logger.Processor.WaitForWrites(4).DefaultTimeout();

        var lines = logger.Processor.Lines;
        Assert.Equal("#Version: 1.0", lines[0]);

        Assert.StartsWith("#Start-Date: ", lines[1]);
        var startDate = DateTime.Parse(lines[1].Substring(13), CultureInfo.InvariantCulture);
        // Assert that the log was written in the last 10 seconds
        // W3CLogger writes start-time to second precision, so delta could be as low as -0.999...
        var delta = startDate.Subtract(now).TotalSeconds;
        Assert.InRange(delta, -1, 10);

        Assert.Equal("#Fields: date time c-ip s-computername s-ip s-port cs-method cs-uri-stem cs-uri-query sc-status time-taken cs-version cs-host cs(User-Agent) cs(Referer)", lines[2]);
        Assert.DoesNotContain(lines[3], "Snickerdoodle");
    }

    [Fact]
    public async Task LogsAdditionalRequestHeaders()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.AdditionalRequestHeaders.Add("x-forwarded-for");
        options.CurrentValue.AdditionalRequestHeaders.Add("x-client-ssl-protocol");
        options.CurrentValue.AdditionalRequestHeaders.Add(":invalid");

        var logger = Helpers.CreateTestW3CLogger(options);

        var middleware = new W3CLoggingMiddleware(
            c =>
            {
                c.Response.StatusCode = 200;
                return Task.CompletedTask;
            },
            options,
            logger);

        options.CurrentValue.AdditionalRequestHeaders.Add("ignored-header-added-after-clone");

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Protocol = "HTTP/1.0";
        httpContext.Request.Headers["Cookie"] = "Snickerdoodle";
        httpContext.Request.Headers["x-forwarded-for"] = "1.3.3.7, 2001:db8:85a3:8d3:1319:8a2e:370:7348";
        httpContext.Response.StatusCode = 200;

        var now = DateTime.UtcNow;
        await middleware.Invoke(httpContext);
        await logger.Processor.WaitForWrites(4).DefaultTimeout();

        var lines = logger.Processor.Lines;
        Assert.Equal("#Version: 1.0", lines[0]);

        Assert.StartsWith("#Start-Date: ", lines[1]);
        var startDate = DateTime.Parse(lines[1].Substring(13), CultureInfo.InvariantCulture);
        // Assert that the log was written in the last 10 seconds
        // W3CLogger writes start-time to second precision, so delta could be as low as -0.999...
        var delta = startDate.Subtract(now).TotalSeconds;
        Assert.InRange(delta, -1, 10);

        Assert.Equal("#Fields: date time c-ip s-computername s-ip s-port cs-method cs-uri-stem cs-uri-query sc-status time-taken cs-version cs-host cs(User-Agent) cs(Referer) cs(:invalid) cs(x-client-ssl-protocol) cs(x-forwarded-for)", lines[2]);
        Assert.DoesNotContain("Snickerdoodle", lines[3]);
        Assert.EndsWith("- - 1.3.3.7,+2001:db8:85a3:8d3:1319:8a2e:370:7348", lines[3]);
    }

    [Fact]
    public async Task LogCookie()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = W3CLoggingFields.Cookie;

        var logger = Helpers.CreateTestW3CLogger(options);

        var middleware = new W3CLoggingMiddleware(
            c =>
            {
                c.Response.StatusCode = 200;
                return Task.CompletedTask;
            },
            options,
            logger);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Protocol = "HTTP/1.0";
        httpContext.Request.Headers["Cookie"] = "Snickerdoodle";
        httpContext.Response.StatusCode = 200;

        var now = DateTime.UtcNow;
        await middleware.Invoke(httpContext);
        await logger.Processor.WaitForWrites(4).DefaultTimeout();

        var lines = logger.Processor.Lines;
        Assert.Equal("Snickerdoodle", lines[3]);
    }

    [Fact]
    public async Task LogsAdditionalRequestHeaders_WithNoOtherOptions()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.AdditionalRequestHeaders.Add("x-forwarded-for");
        options.CurrentValue.LoggingFields = W3CLoggingFields.None;

        var logger = Helpers.CreateTestW3CLogger(options);

        var middleware = new W3CLoggingMiddleware(
            c =>
            {
                c.Response.StatusCode = 200;
                return Task.CompletedTask;
            },
            options,
            logger);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Protocol = "HTTP/1.0";
        httpContext.Request.Headers["Cookie"] = "Snickerdoodle";
        httpContext.Request.Headers["x-forwarded-for"] = "1.3.3.7, 2001:db8:85a3:8d3:1319:8a2e:370:7348";
        httpContext.Response.StatusCode = 200;

        var now = DateTime.UtcNow;
        await middleware.Invoke(httpContext);
        await logger.Processor.WaitForWrites(4).DefaultTimeout();

        var lines = logger.Processor.Lines;
        Assert.Equal("#Version: 1.0", lines[0]);

        Assert.StartsWith("#Start-Date: ", lines[1]);
        Assert.Equal("#Fields: cs(x-forwarded-for)", lines[2]);
        Assert.Equal("1.3.3.7,+2001:db8:85a3:8d3:1319:8a2e:370:7348", lines[3]);
    }

    [Fact]
    public async Task OmitsDuplicateAdditionalRequestHeaders()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = options.CurrentValue.LoggingFields | W3CLoggingFields.Host |
                                             W3CLoggingFields.Referer | W3CLoggingFields.UserAgent |
                                             W3CLoggingFields.Cookie;

        options.CurrentValue.AdditionalRequestHeaders.Add(":invalid");
        options.CurrentValue.AdditionalRequestHeaders.Add("x-forwarded-for");
        options.CurrentValue.AdditionalRequestHeaders.Add("Host");
        options.CurrentValue.AdditionalRequestHeaders.Add("Referer");
        options.CurrentValue.AdditionalRequestHeaders.Add("User-Agent");
        options.CurrentValue.AdditionalRequestHeaders.Add("Cookie");
        options.CurrentValue.AdditionalRequestHeaders.Add("x-client-ssl-protocol");

        var logger = Helpers.CreateTestW3CLogger(options);

        var middleware = new W3CLoggingMiddleware(
            c =>
            {
                c.Response.StatusCode = 200;
                return Task.CompletedTask;
            },
            options,
            logger);

        options.CurrentValue.AdditionalRequestHeaders.Add("ignored-header-added-after-clone");

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Protocol = "HTTP/1.0";
        httpContext.Request.Headers["Cookie"] = "Snickerdoodle";
        httpContext.Request.Headers["x-forwarded-for"] = "1.3.3.7, 2001:db8:85a3:8d3:1319:8a2e:370:7348";
        httpContext.Response.StatusCode = 200;

        var now = DateTime.UtcNow;
        await middleware.Invoke(httpContext);
        await logger.Processor.WaitForWrites(4).DefaultTimeout();

        var lines = logger.Processor.Lines;
        Assert.Equal("#Version: 1.0", lines[0]);

        Assert.StartsWith("#Start-Date: ", lines[1]);
        var startDate = DateTime.Parse(lines[1].Substring(13), CultureInfo.InvariantCulture);
        // Assert that the log was written in the last 10 seconds
        // W3CLogger writes start-time to second precision, so delta could be as low as -0.999...
        var delta = startDate.Subtract(now).TotalSeconds;
        Assert.InRange(delta, -1, 10);

        Assert.Equal("#Fields: date time c-ip s-computername s-ip s-port cs-method cs-uri-stem cs-uri-query sc-status time-taken cs-version cs-host cs(User-Agent) cs(Cookie) cs(Referer) cs(:invalid) cs(x-client-ssl-protocol) cs(x-forwarded-for)", lines[2]);
        Assert.Equal(19, lines[3].Split(' ').Length);
        Assert.Contains("Snickerdoodle", lines[3]);
        Assert.Contains("- - 1.3.3.7,+2001:db8:85a3:8d3:1319:8a2e:370:7348", lines[3]);
    }

    [Fact]
    public async Task TimeTakenIsInMilliseconds()
    {
        var options = CreateOptionsAccessor();
        options.CurrentValue.LoggingFields = W3CLoggingFields.TimeTaken;
        var logger = Helpers.CreateTestW3CLogger(options);

        var middleware = new W3CLoggingMiddleware(
            c =>
            {
                c.Response.StatusCode = 200;
                return Task.CompletedTask;
            },
            options,
            logger);

        var httpContext = new DefaultHttpContext();

        var now = DateTime.UtcNow;
        await middleware.Invoke(httpContext);
        await logger.Processor.WaitForWrites(4).DefaultTimeout();

        var lines = logger.Processor.Lines;
        Assert.Equal("#Version: 1.0", lines[0]);

        Assert.StartsWith("#Start-Date: ", lines[1]);
        var startDate = DateTime.Parse(lines[1].Substring(13), CultureInfo.InvariantCulture);
        // Assert that the log was written in the last 10 seconds
        // W3CLogger writes start-time to second precision, so delta could be as low as -0.999...
        var delta = startDate.Subtract(now).TotalSeconds;
        Assert.InRange(delta, -1, 10);

        Assert.Equal("#Fields: time-taken", lines[2]);
        double num;
        Assert.True(Double.TryParse(lines[3], NumberStyles.Number, CultureInfo.InvariantCulture, out num));
    }

    private IOptionsMonitor<W3CLoggerOptions> CreateOptionsAccessor()
    {
        var options = new W3CLoggerOptions();
        var optionsAccessor = Mock.Of<IOptionsMonitor<W3CLoggerOptions>>(o => o.CurrentValue == options);
        return optionsAccessor;
    }
}

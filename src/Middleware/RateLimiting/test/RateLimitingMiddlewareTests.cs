// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.RateLimiting;
public class RateLimitingMiddlewareTests : LoggedTest
{
    [Fact]
    public void Ctor_ThrowsExceptionsWhenNullArgs()
    {
        var options = CreateOptionsAccessor();
        options.Value.AddLimiter<HttpContext>(new TestPartitionedRateLimiter<HttpContext>());

        Assert.Throws<ArgumentNullException>(() => new RateLimitingMiddleware(
            null,
            new NullLoggerFactory(),
            options));

        Assert.Throws<ArgumentNullException>(() => new RateLimitingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
        null,
        options));
    }

    [Fact]
    public void Ctor_ThrowsExceptionsWhenNullLimiter()
    {
        // Default Options instance has no limiter set
        var ex = Assert.Throws<ArgumentException>(() => new RateLimitingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
        new NullLoggerFactory(),
        CreateOptionsAccessor()));
        Assert.Contains("The value of 'options.Limiter' must not be null.", ex.Message);
    }

    [Fact]
    public void Ctor_ThrowsExceptionsWhenNullOnRejected()
    {
        var options = CreateOptionsAccessor();
        options.Value.OnRejected = null;
        options.Value.AddLimiter<HttpContext>(new TestPartitionedRateLimiter<HttpContext>());
        var ex = Assert.Throws<ArgumentException>(() => new RateLimitingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
        new NullLoggerFactory(),
        options));
        Assert.Contains("The value of 'options.OnRejected' must not be null.", ex.Message);
    }

    [Fact]
    public async Task RequestsCallNextIfAccepted()
    {
        var flag = false;
        var options = CreateOptionsAccessor();
        options.Value.AddLimiter<HttpContext>(new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(true)));
        var middleware = new RateLimitingMiddleware(c =>
        {
            flag = true;
            return Task.CompletedTask;
        },
        new NullLoggerFactory(),
        options);

        await middleware.Invoke(new DefaultHttpContext());
        Assert.True(flag);
    }

    [Fact]
    public async Task RequestRejected_CallsOnRejectedAndGives503()
    {
        bool onRejectedInvoked = false;
        var options = CreateOptionsAccessor();
        options.Value.AddLimiter<HttpContext>(new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(false)));
        options.Value.OnRejected = httpContext =>
        {
            onRejectedInvoked = true;
            return Task.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
        new NullLoggerFactory(),
        options);

        var context = new DefaultHttpContext();
        await middleware.Invoke(context).DefaultTimeout();
        Assert.True(onRejectedInvoked);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    private IOptions<RateLimitingOptions> CreateOptionsAccessor()
    {
        var options = new RateLimitingOptions();
        var optionsAccessor = Mock.Of<IOptions<RateLimitingOptions>>(o => o.Value == options);
        return optionsAccessor;
    }

}

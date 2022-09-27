// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Moq;

#nullable enable
namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Tests;

public class DatabaseDeveloperPageExceptionFilterTests
{
    [Fact]
    public async Task NonDbExceptions_NotHandled()
    {
        var filter = new DatabaseDeveloperPageExceptionFilter(
            NullLogger<DatabaseDeveloperPageExceptionFilter>.Instance,
            Options.Create(new DatabaseErrorPageOptions()));
        var response = new Mock<HttpResponse>();
        response.Setup(r => r.HasStarted).Returns(false);
        var context = new Mock<HttpContext>();
        context.Setup(c => c.Response).Returns(response.Object);
        var nextFilterInvoked = false;

        await filter.HandleExceptionAsync(
            new ErrorContext(context.Object, new InvalidOperationException()),
            context =>
            {
                nextFilterInvoked = true;
                return Task.CompletedTask;
            });

        Assert.True(nextFilterInvoked);
    }

    [Fact]
    public async Task Wrapped_DbExceptions_HandlingFails_InvokesNextFilter()
    {
        var sink = new TestSink();
        var filter = new DatabaseDeveloperPageExceptionFilter(
            new TestLogger<DatabaseDeveloperPageExceptionFilter>(new TestLoggerFactory(sink, true)),
            Options.Create(new DatabaseErrorPageOptions()));
        var context = new DefaultHttpContext();
        var exception = new InvalidOperationException("Bang!", new Mock<DbException>().Object);
        var nextFilterInvoked = false;

        await filter.HandleExceptionAsync(
            new ErrorContext(context, exception),
            context =>
            {
                nextFilterInvoked = true;
                return Task.CompletedTask;
            });

        Assert.True(nextFilterInvoked);
        Assert.Equal(1, sink.Writes.Count);
        var message = sink.Writes.Single();
        Assert.Equal(LogLevel.Error, message.LogLevel);
        Assert.Contains("An exception occurred while calculating the database error page content.", message.Message);
    }

    [Fact]
    public async Task DbExceptions_HandlingFails_InvokesNextFilter()
    {
        var sink = new TestSink();
        var filter = new DatabaseDeveloperPageExceptionFilter(
            new TestLogger<DatabaseDeveloperPageExceptionFilter>(new TestLoggerFactory(sink, true)),
            Options.Create(new DatabaseErrorPageOptions()));
        var context = new DefaultHttpContext();
        var exception = new Mock<DbException>();
        var nextFilterInvoked = false;

        await filter.HandleExceptionAsync(
            new ErrorContext(context, exception.Object),
            context =>
            {
                nextFilterInvoked = true;
                return Task.CompletedTask;
            });

        Assert.True(nextFilterInvoked);
        Assert.Equal(1, sink.Writes.Count);
        var message = sink.Writes.Single();
        Assert.Equal(LogLevel.Error, message.LogLevel);
        Assert.Contains("An exception occurred while calculating the database error page content.", message.Message);
    }

    [Fact]
    public async Task DbExceptions_HandlingFails_ReturnsIfResponseStarted()
    {
        var sink = new TestSink();
        var filter = new DatabaseDeveloperPageExceptionFilter(
            new TestLogger<DatabaseDeveloperPageExceptionFilter>(new TestLoggerFactory(sink, true)),
            Options.Create(new DatabaseErrorPageOptions()));
        var response = new Mock<HttpResponse>();
        response.Setup(r => r.HasStarted).Returns(true);
        var context = new Mock<HttpContext>();
        context.Setup(c => c.Response).Returns(response.Object);
        var exception = new Mock<DbException>();
        var nextFilterInvoked = false;

        await filter.HandleExceptionAsync(
            new ErrorContext(context.Object, exception.Object),
            context =>
            {
                nextFilterInvoked = true;
                return Task.CompletedTask;
            });

        Assert.False(nextFilterInvoked);
        Assert.Contains(sink.Writes, w => w.Message == "The response has already started, the next developer page exception filter will not be executed.");
    }
}

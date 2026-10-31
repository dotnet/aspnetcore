// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Rewrite.Tests;

public class RewriteMiddlewareLoggingTests
{
    [Fact]
    public async Task ContinueRules_LogsCurrentEncodedUrlAtDebug()
    {
        var sink = await RunAsync(new RewriteOptions().AddRewrite("^foo/(.*)$", "bar/$1", skipRemainingRules: false), enabled: true);

        var write = Assert.Single(sink.Writes, w => w.EventId.Name == "RequestContinueResults");
        Assert.Equal(LogLevel.Debug, write.LogLevel);
        Assert.Equal("Request is continuing in applying rules. Current url is http://example.com/base/bar/a%20b?q=1", write.Message);
    }

    [Fact]
    public async Task SkipRemainingRules_LogsRewrittenEncodedUrlAtDebug()
    {
        var sink = await RunAsync(new RewriteOptions().AddRewrite("^foo/(.*)$", "bar/$1", skipRemainingRules: true), enabled: true);

        var write = Assert.Single(sink.Writes, w => w.EventId.Name == "RequestStopRules");
        Assert.Equal("Request is done applying rules. Url was rewritten to http://example.com/base/bar/a%20b?q=1", write.Message);
    }

    [Fact]
    public async Task EndResponse_LogsLocationAndStatusCodeAtDebug()
    {
        var sink = await RunAsync(new RewriteOptions().AddRedirect("^foo/(.*)$", "bar/$1", 301), enabled: true);

        var write = Assert.Single(sink.Writes, w => w.EventId.Name == "RequestResponseComplete");
        Assert.Equal("Request is done processing. Location header '/base/bar/a%20b?q=1' with status code '301'.", write.Message);
    }

    [Fact]
    public async Task DebugDisabled_DoesNotLogRuleResults()
    {
        var sink = await RunAsync(new RewriteOptions().AddRewrite("^foo/(.*)$", "bar/$1", skipRemainingRules: false), enabled: false);

        Assert.DoesNotContain(sink.Writes, w => w.EventId.Name is "RequestContinueResults" or "RequestStopRules" or "RequestResponseComplete");
    }

    private static async Task<TestSink> RunAsync(RewriteOptions options, bool enabled)
    {
        var sink = new TestSink(_ => enabled, _ => enabled);
        var loggerFactory = new TestLoggerFactory(sink, enabled);
        var middleware = new RewriteMiddleware(_ => Task.CompletedTask, new TestWebHostEnvironment(), loggerFactory, Options.Create(options));

        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("example.com");
        context.Request.PathBase = "/base";
        context.Request.Path = "/foo/a b";
        context.Request.QueryString = new QueryString("?q=1");

        await middleware.Invoke(context);
        return sink;
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Test";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Test";
    }
}

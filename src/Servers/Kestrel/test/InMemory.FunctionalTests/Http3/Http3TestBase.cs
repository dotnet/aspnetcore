// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO.Pipelines;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Moq;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public abstract class Http3TestBase : TestApplicationErrorLoggerLoggedTest, IDisposable
{
    protected static readonly int MaxRequestHeaderFieldSize = 16 * 1024;
    protected static readonly string _4kHeaderValue = new string('a', 4096);
    protected static readonly byte[] _helloWorldBytes = Encoding.ASCII.GetBytes("hello, world");
    protected static readonly byte[] _maxData = Encoding.ASCII.GetBytes(new string('a', 16 * 1024));

    internal Http3InMemory Http3Api { get; private set; }

    internal TestServiceContext _serviceContext;
    internal readonly Mock<ITimeoutHandler> _mockTimeoutHandler = new Mock<ITimeoutHandler>();

    protected readonly RequestDelegate _noopApplication;
    protected readonly RequestDelegate _notImplementedApp;
    protected readonly RequestDelegate _echoApplication;
    protected readonly RequestDelegate _readRateApplication;
    protected readonly RequestDelegate _echoMethod;
    protected readonly RequestDelegate _echoPath;
    protected readonly RequestDelegate _echoHost;

    protected static readonly IEnumerable<KeyValuePair<string, string>> _browserRequestHeaders = new[]
    {
        new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
        new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
        new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        new KeyValuePair<string, string>("user-agent", "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:54.0) Gecko/20100101 Firefox/54.0"),
        new KeyValuePair<string, string>("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"),
        new KeyValuePair<string, string>("accept-language", "en-US,en;q=0.5"),
        new KeyValuePair<string, string>("accept-encoding", "gzip, deflate, br"),
        new KeyValuePair<string, string>("upgrade-insecure-requests", "1"),
    };

    protected static IEnumerable<KeyValuePair<string, string>> ReadRateRequestHeaders(int expectedBytes) => new[]
    {
        new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
        new KeyValuePair<string, string>(InternalHeaderNames.Path, "/" + expectedBytes),
        new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
    };

    public Http3TestBase()
    {
        _noopApplication = context => Task.CompletedTask;
        _notImplementedApp = _ => throw new NotImplementedException();

        _echoApplication = async context =>
        {
            var buffer = new byte[16 * 1024];
            var received = 0;

            while ((received = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await context.Response.Body.WriteAsync(buffer, 0, received);
            }
        };

        _readRateApplication = async context =>
        {
            var expectedBytes = int.Parse(context.Request.Path.Value.Substring(1), CultureInfo.InvariantCulture);

            var buffer = new byte[16 * 1024];
            var received = 0;

            while (received < expectedBytes)
            {
                received += await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            }

            var stalledReadTask = context.Request.Body.ReadAsync(buffer, 0, buffer.Length);

            // Write to the response so the test knows the app started the stalled read.
            await context.Response.Body.WriteAsync(new byte[1], 0, 1);

            await stalledReadTask;
        };

        _echoMethod = context =>
        {
            context.Response.Headers["Method"] = context.Request.Method;

            return Task.CompletedTask;
        };

        _echoPath = context =>
        {
            context.Response.Headers["path"] = context.Request.Path.ToString();
            context.Response.Headers["rawtarget"] = context.Features.Get<IHttpRequestFeature>().RawTarget;

            return Task.CompletedTask;
        };

        _echoHost = context =>
        {
            context.Response.Headers.Host = context.Request.Headers.Host;

            return Task.CompletedTask;
        };
    }

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);

        _serviceContext = new TestServiceContext(LoggerFactory)
        {
            Scheduler = PipeScheduler.Inline,
        };

        Http3Api = new Http3InMemory(_serviceContext, _serviceContext.FakeTimeProvider, _mockTimeoutHandler.Object, LoggerFactory);
    }

    public void AssertExpectedErrorMessages(string expectedErrorMessage)
    {
        if (expectedErrorMessage != null)
        {
            Assert.Contains(LogMessages, m => m.Exception?.Message.Contains(expectedErrorMessage) ?? false);
        }
    }

    public void AssertExpectedErrorMessages(Type exceptionType, string[] expectedErrorMessage)
    {
        if (expectedErrorMessage?.Length > 0)
        {
            var message = Assert.Single(LogMessages, m => m.Exception != null && exceptionType.IsAssignableFrom(m.Exception.GetType()));

            Assert.Contains(expectedErrorMessage, expected => message.Exception.Message.Contains(expected));
        }
    }
}

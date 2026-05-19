// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http.Tests.Timeouts;

public class RequestTimeoutsMiddlewareTests
{
    [Fact]
    public async Task DefaultTimeoutWhenNoEndpoint()
    {
        var middleware = CreateMiddleware(expectedTimeSpan: 10, defaultTimeout: 10);

        var context = new DefaultHttpContext();
        var originalToken = context.RequestAborted;

        await middleware.Invoke(context);

        Assert.Equal(originalToken, context.RequestAborted);
    }

    [Fact]
    public async Task DefaultTimeoutWhenNoMetadata()
    {
        var middleware = CreateMiddleware(expectedTimeSpan: 10, defaultTimeout: 10);

        var context = new DefaultHttpContext();
        var originalToken = context.RequestAborted;

        var endpoint = CreateEndpoint();
        context.SetEndpoint(endpoint);

        await middleware.Invoke(context);

        Assert.Equal(originalToken, context.RequestAborted);
    }

    [Fact]
    public async Task TimeoutFromMetadataPolicy()
    {
        var middleware = CreateMiddleware(expectedTimeSpan: 47);

        var context = new DefaultHttpContext();
        var originalToken = context.RequestAborted;

        var endpoint = CreateEndpoint(new RequestTimeoutPolicy { Timeout = TimeSpan.FromSeconds(47) });
        context.SetEndpoint(endpoint);

        await middleware.Invoke(context);

        Assert.Equal(originalToken, context.RequestAborted);
    }

    [Fact]
    public async Task TimeoutFromMetadataAttributeWithPolicy()
    {
        var middleware = CreateMiddleware(expectedTimeSpan: 2);

        var context = new DefaultHttpContext();
        var originalToken = context.RequestAborted;
        var endpoint = CreateEndpoint(new RequestTimeoutAttribute("policy2"));
        context.SetEndpoint(endpoint);

        await middleware.Invoke(context);

        Assert.Equal(originalToken, context.RequestAborted);
    }

    [Fact]
    public async Task TimeoutFromMetadataAttributeWithTimeSpan()
    {
        var middleware = CreateMiddleware(expectedTimeSpan: 3);

        var context = new DefaultHttpContext();
        var originalToken = context.RequestAborted;
        var endpoint = CreateEndpoint(new RequestTimeoutAttribute(3000));
        context.SetEndpoint(endpoint);

        await middleware.Invoke(context);

        Assert.Equal(originalToken, context.RequestAborted);
    }

    [Fact]
    public async Task SkipWhenNoDefaultTimeout()
    {
        var context = new DefaultHttpContext();

        var middleware = CreateMiddleware(
            originalCancellationToken: context.RequestAborted,
            linkerCalled: false,
            timeoutFeatureExists: false);

        var originalToken = context.RequestAborted;

        await middleware.Invoke(context);

        Assert.Equal(originalToken, context.RequestAborted);
    }

    [Fact]
    public async Task TimeoutsAttributeWithPolicyWinsOverDefault()
    {
        var middleware = CreateMiddleware(expectedTimeSpan: 1, defaultTimeout: 10);

        var context = new DefaultHttpContext();
        var originalToken = context.RequestAborted;

        var endpoint = CreateEndpoint(new RequestTimeoutAttribute("policy1"));
        context.SetEndpoint(endpoint);

        await middleware.Invoke(context);

        Assert.Equal(originalToken, context.RequestAborted);
    }

    [Fact]
    public async Task TimeoutsAttributeWithTimeSpanWinsOverDefault()
    {
        var middleware = CreateMiddleware(expectedTimeSpan: 3, defaultTimeout: 10);

        var context = new DefaultHttpContext();
        var originalToken = context.RequestAborted;

        var endpoint = CreateEndpoint(new RequestTimeoutAttribute(3000));
        context.SetEndpoint(endpoint);

        await middleware.Invoke(context);

        Assert.Equal(originalToken, context.RequestAborted);
    }

    [Fact]
    public async Task TimeoutsPolicyWinsOverDefault()
    {
        var middleware = CreateMiddleware(expectedTimeSpan: 47, defaultTimeout: 10);

        var context = new DefaultHttpContext();
        var originalToken = context.RequestAborted;

        var endpoint = CreateEndpoint(new RequestTimeoutPolicy { Timeout = TimeSpan.FromSeconds(47) }, new RequestTimeoutAttribute("policy1"));
        context.SetEndpoint(endpoint);

        await middleware.Invoke(context);

        Assert.Equal(originalToken, context.RequestAborted);
    }

    [Fact]
    public async Task DisableTimeoutAttributeSkipTheMiddleware()
    {
        var context = new DefaultHttpContext();
        var originalToken = context.RequestAborted;

        var middleware = CreateMiddleware(defaultTimeout: 10,
            originalCancellationToken: originalToken,
            linkerCalled: false,
            timeoutFeatureExists: false);

        var endpoint = CreateEndpoint(new DisableRequestTimeoutAttribute(),
            new RequestTimeoutPolicy { Timeout = TimeSpan.FromSeconds(47) },
            new RequestTimeoutAttribute("policy1"));
        context.SetEndpoint(endpoint);

        await middleware.Invoke(context);

        Assert.Equal(originalToken, context.RequestAborted);
    }

    [Fact]
    public async Task ThrowExceptionWhenPolicyNotFound()
    {
        var middleware = CreateMiddleware();

        var context = new DefaultHttpContext();

        var endpoint = CreateEndpoint(new RequestTimeoutAttribute("policy47"));
        context.SetEndpoint(endpoint);

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(context));
    }

    [Fact]
    public async Task HandleTimeoutExceptionDefaultPolicy()
    {
        var middleware = CreateMiddlewareWithCancel(expectedTimeSpan: 10, defaultTimeout: 10);

        var context = new DefaultHttpContext();
        context.Response.Headers.Add("ToBeCleared", "Later");
        var originalToken = context.RequestAborted;

        await middleware.Invoke(context);

        Assert.Equal(StatusCodes.Status418ImATeapot, context.Response.StatusCode);
        Assert.Empty(context.Response.Headers);
        Assert.Equal(originalToken, context.RequestAborted);
    }

    [Fact]
    public async Task HandleTimeoutExceptionFromDefaultPolicy()
    {
        var middleware = CreateMiddlewareWithCancel(expectedTimeSpan: 10, defaultTimeout: 10);

        var context = new DefaultHttpContext();
        context.Response.Headers.Add("ToBeCleared", "Later");
        var originalToken = context.RequestAborted;

        await middleware.Invoke(context);

        Assert.Equal(StatusCodes.Status418ImATeapot, context.Response.StatusCode);
        Assert.Empty(context.Response.Headers);
        Assert.Equal("default", context.Items["SetFrom"]);
        Assert.Equal(originalToken, context.RequestAborted);
    }

    [Fact]
    public async Task HandleTimeoutExceptionFromEndpointPolicy()
    {
        var middleware = CreateMiddlewareWithCancel(expectedTimeSpan: 1, defaultTimeout: 10);

        var context = new DefaultHttpContext();
        context.Response.Headers.Add("ToBeCleared", "Later");
        var originalToken = context.RequestAborted;

        var endpoint = CreateEndpoint(new RequestTimeoutAttribute("policy1"));
        context.SetEndpoint(endpoint);

        await middleware.Invoke(context);

        Assert.Equal(111, context.Response.StatusCode);
        Assert.Empty(context.Response.Headers);
        Assert.Equal("policy1", context.Items["SetFrom"]);
        Assert.Equal(originalToken, context.RequestAborted);
    }

    [Fact]
    public async Task SkipHandleTimeoutException()
    {
        var middleware = CreateMiddlewareWithCancel(expectedTimeSpan: 10, defaultTimeout: 10, cancelledCts: false);

        var context = new DefaultHttpContext();
        context.Response.Headers.Add("NotGonnaBeCleared", "Not Today!");
        var originalToken = context.RequestAborted;

        await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.Invoke(context));

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.NotEmpty(context.Response.Headers);
        Assert.False(context.Items.ContainsKey("SetFrom"));
        Assert.Equal(originalToken, context.RequestAborted);
    }

    private static RequestTimeoutsMiddleware CreateMiddlewareWithCancel(
        double? expectedTimeSpan = null,
        double? defaultTimeout = null,
        bool cancelledCts = true,
        CancellationToken originalCancellationToken = default,
        bool linkerCalled = true)
    {
        return CreateMiddleware(context =>
        {

            throw new OperationCanceledException(context.RequestAborted);
        },
        expectedTimeSpan,
        defaultTimeout,
        cancelledCts,
        originalCancellationToken,
        linkerCalled);
    }

    private static RequestTimeoutsMiddleware CreateMiddleware(
        RequestDelegate requestDelegate = null,
        double? expectedTimeSpan = null,
        double? defaultTimeout = null,
        bool cancelledCts = false,
        CancellationToken originalCancellationToken = default,
        bool linkerCalled = true,
        bool timeoutFeatureExists = true)
    {
        var ctsLinker = new MockCancellationTokenSourceProvider(expectedTimeSpan.HasValue ? TimeSpan.FromSeconds(expectedTimeSpan.Value) : null, cancelledCts);
        var options = new RequestTimeoutOptions
        {
            DefaultPolicy = defaultTimeout.HasValue ? new RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(defaultTimeout.Value),
                TimeoutStatusCode = StatusCodes.Status418ImATeapot,
                WriteTimeoutResponse = context =>
                {
                    context.Items["SetFrom"] = "default";
                    return Task.CompletedTask;
                }
            } : null,
        };
        options.Policies.Add("policy1", new RequestTimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(1),
            TimeoutStatusCode = 111,
            WriteTimeoutResponse = context =>
            {
                context.Items["SetFrom"] = "policy1";
                return Task.CompletedTask;
            }
        });
        options.Policies.Add("policy2", new RequestTimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(2),
            TimeoutStatusCode = 222,
            WriteTimeoutResponse = context =>
            {
                context.Items["SetFrom"] = "policy2";
                return Task.CompletedTask;
            }
        });

        var optionsMonitor = new MiddlewareOptions(options);

        return new RequestTimeoutsMiddleware(requestDelegate ?? next, ctsLinker, NullLogger<RequestTimeoutsMiddleware>.Instance, optionsMonitor);

        Task next(HttpContext context)
        {
            var timeoutFeature = context.Features.Get<IHttpRequestTimeoutFeature>();
            Assert.Equal(timeoutFeatureExists, timeoutFeature is not null);

            Assert.Equal(linkerCalled, ctsLinker.Called);
            if (ctsLinker.Called)
            {
                Assert.Equal(ctsLinker.ReplacedToken, context.RequestAborted);
            }
            else
            {
                Assert.Equal(originalCancellationToken, context.RequestAborted);
            }
            return Task.CompletedTask;
        }
    }

    private static Endpoint CreateEndpoint(params object[] metadata)
    {
        return new Endpoint(null, new EndpointMetadataCollection(metadata), "endpoint");
    }

    private class MockCancellationTokenSourceProvider : ICancellationTokenLinker
    {
        private readonly TimeSpan? _expectedTimeSpan;
        private readonly bool _cancelledCts;

        public CancellationToken ReplacedToken { get; private set; }
        public CancellationTokenSource LinkedCts { get; private set; }

        public bool Called { get; private set; }

        public MockCancellationTokenSourceProvider(TimeSpan? expectedTimeSpan, bool cancelledCts)
        {
            _expectedTimeSpan = expectedTimeSpan;
            _cancelledCts = cancelledCts;
        }

        public (CancellationTokenSource linkedCts, CancellationTokenSource timeoutCts) GetLinkedCancellationTokenSource(HttpContext httpContext, CancellationToken originalToken, TimeSpan timeSpan)
        {
            Assert.Equal(_expectedTimeSpan, timeSpan);

            Called = true;

            var cts = new CancellationTokenSource();
            if (_cancelledCts)
            {
                cts.Cancel();
            }

            ReplacedToken = cts.Token;
            return (cts, new CancellationTokenSource());
        }
    }

    private class MiddlewareOptions : IOptionsMonitor<RequestTimeoutOptions>
    {
        private readonly RequestTimeoutOptions _options;

        public MiddlewareOptions(RequestTimeoutOptions options)
        {
            _options = options;
        }
        public RequestTimeoutOptions CurrentValue => _options;

        public RequestTimeoutOptions Get(string name) => _options;

        public IDisposable OnChange(Action<RequestTimeoutOptions, string> listener)
        {
            return default;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http;

public class RequestTimeoutsMiddlewareBenchmark
{
    RequestTimeoutsMiddleware _middlewareWithNoTimeout;
    RequestTimeoutsMiddleware _middleware;
    RequestTimeoutsMiddleware _middlewareWithThrow;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _middlewareWithNoTimeout = new RequestTimeoutsMiddleware(
            async context => { await Task.Yield(); },
            new CancellationTokenLinker(),
            NullLogger<RequestTimeoutsMiddleware>.Instance,
            Options.Create(new RequestTimeoutOptions()));

        _middleware = new RequestTimeoutsMiddleware(
          async context => { await Task.Yield(); },
          new CancellationTokenLinker(),
          NullLogger<RequestTimeoutsMiddleware>.Instance,
          Options.Create(new RequestTimeoutOptions
          {
              DefaultPolicy = new RequestTimeoutPolicy
              {
                  Timeout = TimeSpan.FromMilliseconds(200)
              },
              Policies =
              {
                  ["policy1"] = new RequestTimeoutPolicy { Timeout = TimeSpan.FromMilliseconds(200)}
              }
          }));

        _middlewareWithThrow = new RequestTimeoutsMiddleware(
          async context =>
          {
              await Task.Delay(TimeSpan.FromMicroseconds(2));
              context.RequestAborted.ThrowIfCancellationRequested();
          },
          new CancellationTokenLinker(),
          NullLogger<RequestTimeoutsMiddleware>.Instance,
          Options.Create(new RequestTimeoutOptions
          {
              DefaultPolicy = new RequestTimeoutPolicy
              {
                  Timeout = TimeSpan.FromMicroseconds(1)
              }
          }));
    }

    [Benchmark]
    public async Task NoMetadataNoDefault()
    {
        var context = CreateHttpContext(new Endpoint(null, null, null));
        await _middlewareWithNoTimeout.Invoke(context);
    }

    [Benchmark]
    public async Task DefaultTimeout()
    {
        var context = CreateHttpContext(new Endpoint(null, null, null));

        await _middleware.Invoke(context);
    }

    [Benchmark]
    public async Task DefaultTimeoutOverriddenByDisable()
    {
        var context = CreateHttpContext(new Endpoint(
            null,
            new EndpointMetadataCollection(new DisableRequestTimeoutAttribute()),
            null));

        await _middleware.Invoke(context);
    }

    [Benchmark]
    public async Task TimeoutMetadata()
    {
        var context = CreateHttpContext(new Endpoint(
            null,
            new EndpointMetadataCollection(new RequestTimeoutAttribute(200)),
            null));

        await _middleware.Invoke(context);
    }

    [Benchmark]
    public async Task NamedPolicyMetadata()
    {
        var context = CreateHttpContext(new Endpoint(
            null,
            new EndpointMetadataCollection(new RequestTimeoutAttribute("policy1")),
            null));

        await _middleware.Invoke(context);
    }

    [Benchmark]
    public async Task TimeoutFires()
    {
        var context = CreateHttpContext(new Endpoint(null, null, null));

        await _middlewareWithThrow.Invoke(context);
    }

    private HttpContext CreateHttpContext(Endpoint endpoint)
    {
        var context = new DefaultHttpContext();
        context.SetEndpoint(endpoint);

        var cts = new CancellationTokenSource();
        context.RequestAborted = cts.Token;

        return context;
    }

    private sealed class Options : IOptionsMonitor<RequestTimeoutOptions>
    {
        private readonly RequestTimeoutOptions _options;

        private Options(RequestTimeoutOptions options)
        {
            _options = options;
        }

        public static Options Create(RequestTimeoutOptions options)
        {
            return new Options(options);
        }

        public RequestTimeoutOptions CurrentValue => _options;

        public RequestTimeoutOptions Get(string name) => _options;

        public IDisposable OnChange(Action<RequestTimeoutOptions, string> listener)
        {
            return default;
        }
    }
}

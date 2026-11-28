// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.OutputCaching.Tests;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.NewResponseCaching.Tests;

/// <summary>
/// Mirrors the Response Caching test application, but hosts it with Output Caching configured with <see cref="OutputCachePolicyBuilder.UseResponseCachingSemantics"/>.
/// </summary>
internal static class ResponseCachingTestUtils
{
    static ResponseCachingTestUtils()
    {
        // Force sharding in tests
        StreamUtilities.BodySegmentSize = 10;
    }

    private static bool TestRequestDelegate(HttpContext context, string guid)
    {
        var headers = context.Response.GetTypedHeaders();

        var expires = context.Request.Query["Expires"];
        if (!string.IsNullOrEmpty(expires))
        {
            headers.Expires = DateTimeOffset.Now.AddSeconds(int.Parse(expires, CultureInfo.InvariantCulture));
        }

        if (headers.CacheControl == null)
        {
            headers.CacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = string.IsNullOrEmpty(expires) ? TimeSpan.FromSeconds(10) : (TimeSpan?)null
            };
        }
        else
        {
            headers.CacheControl.Public = true;
            headers.CacheControl.MaxAge = string.IsNullOrEmpty(expires) ? TimeSpan.FromSeconds(10) : (TimeSpan?)null;
        }
        headers.Date = DateTimeOffset.UtcNow;
        headers.Headers["X-Value"] = guid;

        var contentLength = context.Request.Query["ContentLength"];
        if (!string.IsNullOrEmpty(contentLength))
        {
            headers.ContentLength = long.Parse(contentLength, CultureInfo.InvariantCulture);
        }

        if (context.Request.Method != "HEAD")
        {
            return true;
        }
        return false;
    }

    internal static async Task TestRequestDelegateWriteAsync(HttpContext context)
    {
        var uniqueId = Guid.NewGuid().ToString();
        if (TestRequestDelegate(context, uniqueId))
        {
            await context.Response.WriteAsync(uniqueId);
        }
    }

    internal static async Task TestRequestDelegateSendFileAsync(HttpContext context)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestDocument.txt");
        var uniqueId = Guid.NewGuid().ToString();
        if (TestRequestDelegate(context, uniqueId))
        {
            await context.Response.SendFileAsync(path, 0, null);
            await context.Response.WriteAsync(uniqueId);
        }
    }

    internal static Task TestRequestDelegateWrite(HttpContext context)
    {
        var uniqueId = Guid.NewGuid().ToString();
        if (TestRequestDelegate(context, uniqueId))
        {
            var feature = context.Features.Get<IHttpBodyControlFeature>();
            if (feature != null)
            {
                feature.AllowSynchronousIO = true;
            }
            context.Response.Write(uniqueId);
        }
        return Task.CompletedTask;
    }

    internal static IEnumerable<IHostBuilder> CreateBuildersWithResponseCaching(
        Action<IApplicationBuilder> configureDelegate = null,
        ResponseCachingOptions options = null,
        Action<HttpContext> contextAction = null,
        Action<OutputCachePolicyBuilder> configurePolicy = null)
    {
        Action<HttpContext> action = context => contextAction?.Invoke(context);

        return CreateBuildersWithResponseCaching(configureDelegate, options, configurePolicy, new RequestDelegate[]
            {
                    context =>
                    {
                        action(context);
                        return TestRequestDelegateWrite(context);
                    },
                    context =>
                    {
                        action(context);
                        return TestRequestDelegateWriteAsync(context);
                    },
                    context =>
                    {
                        action(context);
                        return TestRequestDelegateSendFileAsync(context);
                    },
            });
    }

    private static IEnumerable<IHostBuilder> CreateBuildersWithResponseCaching(
        Action<IApplicationBuilder> configureDelegate,
        ResponseCachingOptions options,
        Action<OutputCachePolicyBuilder> configurePolicy,
        IEnumerable<RequestDelegate> requestDelegates)
    {
        configureDelegate ??= app => { };

        foreach (var requestDelegate in requestDelegates)
        {
            yield return new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddOutputCache(outputCacheOptions =>
                        {
                            if (options != null)
                            {
                                outputCacheOptions.SizeLimit = options.SizeLimit;
                                outputCacheOptions.MaximumBodySize = options.MaximumBodySize;
                                outputCacheOptions.UseCaseSensitivePaths = options.UseCaseSensitivePaths;
                            }

                            outputCacheOptions.AddBasePolicy(policy =>
                            {
                                policy.UseResponseCachingSemantics();
                                configurePolicy?.Invoke(policy);
                            });
                        });
                    })
                    .Configure(app =>
                    {
                        configureDelegate(app);
                        app.UseOutputCache();
                        app.Run(requestDelegate);
                    });
                });
        }
    }

    internal static OutputCacheContext CreateTestContext()
    {
        return new OutputCacheContext
        {
            HttpContext = new DefaultHttpContext(),
            ResponseTime = DateTimeOffset.UtcNow
        };
    }

    public static HttpRequestMessage CreateRequest(string method, string requestUri)
    {
        return new HttpRequestMessage(new HttpMethod(method), requestUri);
    }
}

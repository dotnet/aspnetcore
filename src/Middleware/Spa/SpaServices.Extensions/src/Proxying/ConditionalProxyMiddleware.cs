// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.SpaServices.Extensions.Proxy;

// This duplicates and updates the proxying logic in SpaServices so that we can update
// the project templates without waiting for 2.1 to ship. When 2.1 is ready to ship,
// merge the additional proxying features (e.g., proxying websocket connections) back
// into the SpaServices proxying code. It's all internal.
internal sealed class ConditionalProxyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Task<Uri> _baseUriTask;
    private readonly string _pathPrefix;
    private readonly bool _pathPrefixIsRoot;
    private readonly HttpClient _httpClient;
    private readonly CancellationToken _applicationStoppingToken;

    public ConditionalProxyMiddleware(
        RequestDelegate next,
        string pathPrefix,
        TimeSpan requestTimeout,
        Task<Uri> baseUriTask,
        IHostApplicationLifetime applicationLifetime)
    {
        if (!pathPrefix.StartsWith('/'))
        {
            pathPrefix = "/" + pathPrefix;
        }

        _next = next;
        _pathPrefix = pathPrefix;
        _pathPrefixIsRoot = string.Equals(_pathPrefix, "/", StringComparison.Ordinal);
        _baseUriTask = baseUriTask;
        _httpClient = SpaProxy.CreateHttpClientForProxy(requestTimeout);
        _applicationStoppingToken = applicationLifetime.ApplicationStopping;
    }

    public Task Invoke(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments(_pathPrefix) || _pathPrefixIsRoot)
        {
            return InvokeCore(context);
        }
        return _next.Invoke(context);
    }

    private async Task InvokeCore(HttpContext context)
    {
        var didProxyRequest = await SpaProxy.PerformProxyRequest(
            context, _httpClient, _baseUriTask, _applicationStoppingToken, proxy404s: false);
        if (didProxyRequest)
        {
            return;
        }

        // Not a request we can proxy
        await _next.Invoke(context);
    }
}

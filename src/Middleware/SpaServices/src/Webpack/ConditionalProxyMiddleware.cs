// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.SpaServices.Webpack
{
    /// <summary>
    /// Based on ProxyMiddleware from https://github.com/aspnet/Proxy/.
    /// Differs in that, if the proxied request returns a 404, we pass through to the next middleware in the chain
    /// This is useful for Webpack middleware, because it lets you fall back on prebuilt files on disk for
    /// chunks not exposed by the current Webpack config (e.g., DLL/vendor chunks).
    /// </summary>
    [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    internal class ConditionalProxyMiddleware
    {
        private const int DefaultHttpBufferSize = 4096;

        private readonly HttpClient _httpClient;
        private readonly RequestDelegate _next;
        private readonly ConditionalProxyMiddlewareOptions _options;
        private readonly string _pathPrefix;
        private readonly bool _pathPrefixIsRoot;

        public ConditionalProxyMiddleware(
            RequestDelegate next,
            string pathPrefix,
            ConditionalProxyMiddlewareOptions options)
        {
            if (!pathPrefix.StartsWith("/"))
            {
                pathPrefix = "/" + pathPrefix;
            }

            _next = next;
            _pathPrefix = pathPrefix;
            _pathPrefixIsRoot = string.Equals(_pathPrefix, "/", StringComparison.Ordinal);
            _options = options;
            _httpClient = new HttpClient(new HttpClientHandler());
            _httpClient.Timeout = _options.RequestTimeout;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments(_pathPrefix) || _pathPrefixIsRoot)
            {
                var didProxyRequest = await PerformProxyRequest(context);
                if (didProxyRequest)
                {
                    return;
                }
            }

            // Not a request we can proxy
            await _next.Invoke(context);
        }

        private async Task<bool> PerformProxyRequest(HttpContext context)
        {
            var requestMessage = new HttpRequestMessage();

            // Copy the request headers
            foreach (var header in context.Request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            requestMessage.Headers.Host = _options.Host + ":" + _options.Port;
            var uriString =
                $"{_options.Scheme}://{_options.Host}:{_options.Port}{context.Request.Path}{context.Request.QueryString}";
            requestMessage.RequestUri = new Uri(uriString);
            requestMessage.Method = new HttpMethod(context.Request.Method);

            using (
                var responseMessage = await _httpClient.SendAsync(
                    requestMessage,
                    HttpCompletionOption.ResponseHeadersRead,
                    context.RequestAborted))
            {
                if (responseMessage.StatusCode == HttpStatusCode.NotFound)
                {
                    // Let some other middleware handle this
                    return false;
                }

                // We can handle this
                context.Response.StatusCode = (int)responseMessage.StatusCode;
                foreach (var header in responseMessage.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                foreach (var header in responseMessage.Content.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                // SendAsync removes chunking from the response. This removes the header so it doesn't expect a chunked response.
                context.Response.Headers.Remove("transfer-encoding");

                using (var responseStream = await responseMessage.Content.ReadAsStreamAsync())
                {
                    try
                    {
                        await responseStream.CopyToAsync(context.Response.Body, DefaultHttpBufferSize, context.RequestAborted);
                    }
                    catch (OperationCanceledException)
                    {
                        // The CopyToAsync task will be canceled if the client disconnects (e.g., user
                        // closes or refreshes the browser tab). Don't treat this as an error.
                    }
                }

                return true;
            }
        }
    }
}

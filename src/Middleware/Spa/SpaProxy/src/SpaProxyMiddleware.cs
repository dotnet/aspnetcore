// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.SpaProxy
{
    /// <summary>
    /// Middleware to display a page while the SPA proxy is launching and redirect to the proxy url once the proxy is
    /// ready or we have given up trying to start it.
    /// This is to help Visual Studio work well in several scenarios by allowing VS to:
    /// 1) Launch on the URL configured for the backend (we handle the redirect to the proxy when ready).
    /// 2) Ensure that the server is up and running quickly instead of waiting for the proxy to be ready to start the
    ///    server which causes Visual Studio to think the app failed to launch.
    /// </summary>
    internal class SpaProxyMiddleware
    {
        private const string SpaLaunchPage = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset = ""UTF-8"" >
  <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
  <meta http-equiv=""refresh"" content=""3"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>SPA proxy launch page</title>
</head>
<body>
  <h1>Launching the SPA proxy...</h1>
  <p>This page will automatically refresh in one second.</p>
</body>
</html>";

        private readonly RequestDelegate _next;
        private readonly SpaProxyStatus _status;
        private readonly IOptions<SpaDevelopmentServerOptions> _options;

        public SpaProxyMiddleware(RequestDelegate next, SpaProxyStatus status, IOptions<SpaDevelopmentServerOptions> options)
        {
            if (next is null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (status is null)
            {
                throw new ArgumentNullException(nameof(status));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next;
            _status = status;
            _options = options;
        }

        public async Task Invoke(HttpContext context)
        {
            var status = context.RequestServices.GetRequiredService<SpaProxyStatus>();
            if (context.Request.Path.Equals(new Uri(_options.Value.ServerUrl).LocalPath))
            {
                if (!status.IsReady)
                {
                    context.Response.ContentType = "text/html";
                    await using var writer = new StreamWriter(context.Response.Body, Encoding.UTF8);
                    await writer.WriteAsync(SpaLaunchPage);
                }
                else
                {
                    context.Response.Headers[HeaderNames.CacheControl] = "no-cache, no-store, must-revalidate, max-age=0";
                    context.Response.Redirect(_options.Value.ServerUrl);
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.SpaProxy;

/// <summary>
/// Middleware to display a page while the SPA proxy is launching and redirect to the proxy url once the proxy is
/// ready or we have given up trying to start it.
/// This is to help Visual Studio work well in several scenarios by allowing VS to:
/// 1) Launch on the URL configured for the backend (we handle the redirect to the proxy when ready).
/// 2) Ensure that the server is up and running quickly instead of waiting for the proxy to be ready to start the
///    server which causes Visual Studio to think the app failed to launch.
/// </summary>
internal sealed class SpaProxyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SpaProxyLaunchManager _spaProxyLaunchManager;
    private readonly IOptions<SpaDevelopmentServerOptions> _options;
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly ILogger<SpaProxyMiddleware> _logger;

    public SpaProxyMiddleware(
        RequestDelegate next,
        SpaProxyLaunchManager spaProxyLaunchManager,
        IOptions<SpaDevelopmentServerOptions> options,
        IHostApplicationLifetime hostLifetime,
        ILogger<SpaProxyMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _spaProxyLaunchManager = spaProxyLaunchManager ?? throw new ArgumentNullException(nameof(spaProxyLaunchManager));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _hostLifetime = hostLifetime ?? throw new ArgumentNullException(nameof(hostLifetime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task Invoke(HttpContext context)
    {
        if (context.Request.Path.Equals(new Uri(_options.Value.ServerUrl).LocalPath))
        {
            return InvokeCore(context);
        }
        return _next(context);
    }

    private async Task InvokeCore(HttpContext context)
    {
        context.Response.Headers[HeaderNames.CacheControl] = "no-cache, no-store, must-revalidate, max-age=0";
        if (!await _spaProxyLaunchManager.IsSpaProxyRunning(context.RequestAborted))
        {
            _spaProxyLaunchManager.StartInBackground(_hostLifetime.ApplicationStopping);
            _logger.LogInformation("SPA proxy is not ready. Returning temporary landing page.");
            context.Response.ContentType = "text/html";

            await using var writer = new StreamWriter(context.Response.Body, Encoding.UTF8);
            await writer.WriteAsync(GenerateSpaLaunchPage(_options.Value));
        }
        else
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation($"SPA proxy is ready. Redirecting to {_options.Value.GetRedirectUrl()}.");
            }
            context.Response.Redirect(_options.Value.GetRedirectUrl());
        }

        static string GenerateSpaLaunchPage(SpaDevelopmentServerOptions options)
        {
            return $@"
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
  <p>This page will automatically redirect to <a href=""{HtmlEncoder.Default.Encode(options.GetRedirectUrl())}"">{HtmlEncoder.Default.Encode(options.GetRedirectUrl())}</a> when the SPA proxy is ready.</p>
</body>
</html>";
        }
    }
}

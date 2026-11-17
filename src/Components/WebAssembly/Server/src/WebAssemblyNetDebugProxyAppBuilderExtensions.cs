// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Server;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides infrastructure for debugging Blazor WebAssembly applications.
/// </summary>
public static class WebAssemblyNetDebugProxyAppBuilderExtensions
{
    /// <summary>
    /// Adds middleware needed for debugging Blazor WebAssembly applications
    /// inside Chromium dev tools.
    /// </summary>
    [Obsolete(Obsoletions.UseWebAssemblyDebuggingMessage, DiagnosticId = Obsoletions.UseWebAssemblyDebuggingDiagId, UrlFormat = Obsoletions.AspNetCoreDeprecate011Url)]
    public static void UseWebAssemblyDebugging(this IApplicationBuilder app)
    {
        app.Map("/_framework/debug", app =>
        {
            app.Run(async (context) =>
            {
                var queryParams = HttpUtility.ParseQueryString(context.Request.QueryString.Value!);
                // The browser query parameter contains the URL that Chrome or Edge exposes
                // for debugger connections. It is not the URL of the Blazor application.
                // This route starts or reuses a .NET debug proxy configured to use that URL.
                // It does not start Chrome or Edge or enable debugging in the browser.
                var browserParam = queryParams.Get("browser");
                Uri? browserUrl = null;
                var devToolsHost = "http://localhost:9222";
                if (browserParam != null)
                {
                    browserUrl = new Uri(browserParam);
                    devToolsHost = $"http://{browserUrl.Host}:{browserUrl.Port}";
                }
                var isFirefox = string.IsNullOrEmpty(queryParams.Get("isFirefox")) ? false : true;
                if (isFirefox)
                {
                    devToolsHost = "localhost:6000";
                }
                var debugProxyBaseUrl = await DebugProxyLauncher.EnsureLaunchedAndGetUrl(context.RequestServices, devToolsHost, isFirefox);
                var requestPath = context.Request.Path.ToString();
                if (requestPath == string.Empty)
                {
                    requestPath = "/";
                }

                switch (requestPath)
                {
                    case "/":
                        var targetPickerUi = new TargetPickerUi(debugProxyBaseUrl, devToolsHost);
                        if (isFirefox)
                        {
                            await targetPickerUi.DisplayFirefox(context);
                        }
                        else
                        {
                            await targetPickerUi.Display(context);
                        }
                        break;
                    case "/ws-proxy":
                        context.Response.Redirect($"{debugProxyBaseUrl}{browserUrl!.PathAndQuery}");
                        break;
                    default:
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        break;
                }
            });
        });
    }
}

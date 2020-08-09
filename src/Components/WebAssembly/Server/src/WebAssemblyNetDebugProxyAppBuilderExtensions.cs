// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using Microsoft.AspNetCore.Components.WebAssembly.Server;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides infrastructure for debugging Blazor WebAssembly applications.
    /// </summary>
    public static class WebAssemblyNetDebugProxyAppBuilderExtensions
    {
        /// <summary>
        /// Adds middleware needed for debugging Blazor WebAssembly applications
        /// inside Chromium dev tools.
        /// </summary>
        public static void UseWebAssemblyDebugging(this IApplicationBuilder app)
        {
            app.Map("/_framework/debug", app =>
            {
                app.Use(async (context, next) =>
                {
                    var debugProxyBaseUrl = await DebugProxyLauncher.EnsureLaunchedAndGetUrl(context.RequestServices);
                    var requestPath = context.Request.Path.ToString();
                    if (requestPath == string.Empty)
                    {
                        requestPath = "/";
                    }

                    switch (requestPath)
                    {
                        case "/":
                            var targetPickerUi = new TargetPickerUi(debugProxyBaseUrl);
                            await targetPickerUi.Display(context);
                            break;
                        case "/ws-proxy":
                            context.Response.Redirect($"{debugProxyBaseUrl}{requestPath}{context.Request.QueryString}");
                            break;
                        default:
                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            break;
                    }
                });
            });
        }
    }
}

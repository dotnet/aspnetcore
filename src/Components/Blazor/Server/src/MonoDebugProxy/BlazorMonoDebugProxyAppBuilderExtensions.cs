// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using WsProxy;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides infrastructure for debugging Blazor applications.
    /// </summary>
    public static class BlazorMonoDebugProxyAppBuilderExtensions
    {
        /// <summary>
        /// Adds middleware for needed for debugging Blazor applications
        /// inside Chromium dev tools.
        /// </summary>
        public static void UseBlazorDebugging(this IApplicationBuilder app)
        {
            app.UseWebSockets();

            app.Use((context, next) =>
            {
                var requestPath = context.Request.Path;
                if (!requestPath.StartsWithSegments("/_framework/debug"))
                {
                    return next();
                }

                if (requestPath.Equals("/_framework/debug/ws-proxy", StringComparison.OrdinalIgnoreCase))
                {
                    return DebugWebSocketProxyRequest(context);
                }

                if (requestPath.Equals("/_framework/debug", StringComparison.OrdinalIgnoreCase))
                {
                    return DebugHome(context);
                }

                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return Task.CompletedTask;
            });
        }

        private static async Task DebugWebSocketProxyRequest(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            var browserUri = new Uri(context.Request.Query["browser"]);
            var ideSocket = await context.WebSockets.AcceptWebSocketAsync();
            await new MonoProxy().Run(browserUri, ideSocket);
        }

        private static async Task DebugHome(HttpContext context)
        {
            context.Response.ContentType = "text/html";

            var request = context.Request;
            var appRootUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/";
            var targetTabUrl = request.Query["url"];
            if (string.IsNullOrEmpty(targetTabUrl))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync("No value specified for 'url'");
                return;
            }

            // TODO: Allow overriding port (but not hostname, as we're connecting to the
            // local browser, not to the webserver serving the app)
            var debuggerHost = "http://localhost:9222";
            var debuggerTabsListUrl = $"{debuggerHost}/json";
            IEnumerable<BrowserTab> availableTabs;

            try
            {
                availableTabs = await GetOpenedBrowserTabs(debuggerHost);
            }
            catch (Exception ex)
            {
                await context.Response.WriteAsync($@"
<h1>Unable to find debuggable browser tab</h1>
<p>
    Could not get a list of browser tabs from <code>{debuggerTabsListUrl}</code>.
    Ensure your browser is running with debugging enabled.
</p>
<h2>Resolution</h2>
<p>
    <h4>If you are using Google Chrome for your development, follow these instructions:</h4>
    {GetLaunchChromeInstructions(appRootUrl)}
</p>
<p>
    <h4>If you are using Microsoft Edge (Chromium) for your development, follow these instructions:</h4>
    {GetLaunchEdgeInstructions(appRootUrl)}
</p>
<strong>This should launch a new browser window with debugging enabled..</p>
<h2>Underlying exception:</h2>
<pre>{ex}</pre>
                ");

                return;
            }

            var matchingTabs = availableTabs
                .Where(t => t.Url.Equals(targetTabUrl, StringComparison.Ordinal))
                .ToList();
            if (matchingTabs.Count == 0)
            {
                await context.Response.WriteAsync($@"
                    <h1>Unable to find debuggable browser tab</h1>
                    <p>
                        The response from <code>{debuggerTabsListUrl}</code> does not include
                        any entry for <code>{targetTabUrl}</code>.
                    </p>");
                return;
            }
            else if (matchingTabs.Count > 1)
            {
                // TODO: Automatically disambiguate by adding a GUID to the page title
                // when you press the debugger hotkey, include it in the querystring passed
                // here, then remove it once the debugger connects.
                await context.Response.WriteAsync($@"
                    <h1>Multiple matching tabs are open</h1>
                    <p>
                        There is more than one browser tab at <code>{targetTabUrl}</code>.
                        Close the ones you do not wish to debug, then refresh this page.
                    </p>");
                return;
            }

            // Now we know uniquely which tab to debug, construct the URL to the debug
            // page and redirect there
            var tabToDebug = matchingTabs.Single();
            var underlyingV8Endpoint = tabToDebug.WebSocketDebuggerUrl;
            var proxyEndpoint = $"{request.Host}{request.PathBase}/_framework/debug/ws-proxy?browser={WebUtility.UrlEncode(underlyingV8Endpoint)}";
            var devToolsUrlAbsolute = new Uri(debuggerHost + tabToDebug.DevtoolsFrontendUrl);
            var devToolsUrlWithProxy = $"{devToolsUrlAbsolute.Scheme}://{devToolsUrlAbsolute.Authority}{devToolsUrlAbsolute.AbsolutePath}?ws={proxyEndpoint}";
            context.Response.Redirect(devToolsUrlWithProxy);
        }

        private static string GetLaunchChromeInstructions(string appRootUrl)
        {
            var profilePath = Path.Combine(Path.GetTempPath(), "blazor-edge-debug");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $@"<p>Press Win+R and enter the following:</p>
                          <p><strong><code>chrome --remote-debugging-port=9222 --user-data-dir=""{profilePath}"" {appRootUrl}</code></strong></p>";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return $@"<p>In a terminal window execute the following:</p>
                          <p><strong><code>google-chrome --remote-debugging-port=9222 --user-data-dir={profilePath} {appRootUrl}</code></strong></p>";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return $@"<p>Execute the following:</p>
                          <p><strong><code>open /Applications/Google\ Chrome.app --args --remote-debugging-port=9222 --user-data-dir={profilePath} {appRootUrl}</code></strong></p>";
            }
            else
            {
                throw new InvalidOperationException("Unknown OS platform");
            }
        }

        private static string GetLaunchEdgeInstructions(string appRootUrl)
        {
            var profilePath = Path.Combine(Path.GetTempPath(), "blazor-chrome-debug");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $@"<p>Press Win+R and enter the following:</p>
                          <p><strong><code>msedge --remote-debugging-port=9222 --user-data-dir=""{profilePath}"" {appRootUrl}</code></strong></p>";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return $@"<p>In a terminal window execute the following:</p>
                          <p><strong><code>open /Applications/Microsoft\ Edge\ Dev.app --args --remote-debugging-port=9222 --user-data-dir={profilePath} {appRootUrl}</code></strong></p>";
            }
            else
            {
                throw new InvalidOperationException("Unknown OS platform");
            }
        }

        private static async Task<IEnumerable<BrowserTab>> GetOpenedBrowserTabs(string debuggerHost)
        {
            using (var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
            {
                var jsonResponse = await httpClient.GetStringAsync($"{debuggerHost}/json");
                return JsonConvert.DeserializeObject<BrowserTab[]>(jsonResponse);
            }
        }

        class BrowserTab
        {
            public string Type { get; set; }
            public string Url { get; set; }
            public string Title { get; set; }
            public string DevtoolsFrontendUrl { get; set; }
            public string WebSocketDebuggerUrl { get; set; }
        }
    }
}

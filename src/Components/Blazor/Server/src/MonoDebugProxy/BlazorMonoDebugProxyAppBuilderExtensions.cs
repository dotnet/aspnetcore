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
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WsProxy;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides infrastructure for debugging Blazor applications.
    /// </summary>
    public static class BlazorMonoDebugProxyAppBuilderExtensions
    {
        private static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            IgnoreNullValues = true
        };

        private static string DefaultDebuggerHost = "http://localhost:9222";

        /// <summary>
        /// Adds middleware for needed for debugging Blazor applications
        /// inside Chromium dev tools.
        /// </summary>
        public static void UseBlazorDebugging(this IApplicationBuilder app)
        {
            app.UseWebSockets();

            app.UseVisualStudioDebuggerConnectionRequestHandlers();

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

        private static string GetDebuggerHost()
        {
            var envVar = Environment.GetEnvironmentVariable("ASPNETCORE_WEBASSEMBLYDEBUGHOST");

            if (string.IsNullOrEmpty(envVar))
            {
                return DefaultDebuggerHost;
            }
            else
            {
                return envVar;
            }
        }

        private static int GetDebuggerPort()
        {
            var host = GetDebuggerHost();
            return new Uri(host).Port;
        }

        private static void UseVisualStudioDebuggerConnectionRequestHandlers(this IApplicationBuilder app)
        {
            // Unfortunately VS doesn't send any deliberately distinguishing information so we know it's
            // not a regular browser or API client. The closest we can do is look for the *absence* of a
            // User-Agent header. In the future, we should try to get VS to send a special header to indicate
            // this is a debugger metadata request.
            app.Use(async (context, next) =>
            {
                var request = context.Request;
                var requestPath = request.Path;
                if (requestPath.StartsWithSegments("/json")
                    && !request.Headers.ContainsKey("User-Agent"))
                {
                    if (requestPath.Equals("/json", StringComparison.OrdinalIgnoreCase) || requestPath.Equals("/json/list", StringComparison.OrdinalIgnoreCase))
                    {
                        var availableTabs = await GetOpenedBrowserTabs();

                        // Filter the list to only include tabs displaying the requested app,
                        // but only during the "choose application to debug" phase. We can't apply
                        // the same filter during the "connecting" phase (/json/list), nor do we need to.
                        if (requestPath.Equals("/json"))
                        {
                            availableTabs = availableTabs.Where(tab => tab.Url.StartsWith($"{request.Scheme}://{request.Host}{request.PathBase}/"));
                        }

                        var proxiedTabInfos = availableTabs.Select(tab =>
                        {
                            var underlyingV8Endpoint = tab.WebSocketDebuggerUrl;
                            var proxiedV8Endpoint = $"ws://{request.Host}{request.PathBase}/_framework/debug/ws-proxy?browser={WebUtility.UrlEncode(underlyingV8Endpoint)}";
                            return new
                            {
                                description = "",
                                devtoolsFrontendUrl = "",
                                id = tab.Id,
                                title = tab.Title,
                                type = tab.Type,
                                url = tab.Url,
                                webSocketDebuggerUrl = proxiedV8Endpoint
                            };
                        });

                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(proxiedTabInfos));
                    }
                    else if (requestPath.Equals("/json/version", StringComparison.OrdinalIgnoreCase))
                    {
                        var browserVersionJson = await GetBrowserVersionInfoAsync();

                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(browserVersionJson);
                    }
                }
                else
                {
                    await next();
                }
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
            var debuggerHost = GetDebuggerHost();
            var debuggerTabsListUrl = $"{debuggerHost}/json";
            IEnumerable<BrowserTab> availableTabs;

            try
            {
                availableTabs = await GetOpenedBrowserTabs();
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
            var wsParamName = request.IsHttps ? "wss" : "ws";
            var devToolsUrlWithProxy = $"{devToolsUrlAbsolute.Scheme}://{devToolsUrlAbsolute.Authority}{devToolsUrlAbsolute.AbsolutePath}?{wsParamName}={proxyEndpoint}";
            context.Response.Redirect(devToolsUrlWithProxy);
        }

        private static string GetLaunchChromeInstructions(string appRootUrl)
        {
            var profilePath = Path.Combine(Path.GetTempPath(), "blazor-chrome-debug");
            var debuggerPort = GetDebuggerPort();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $@"<p>Press Win+R and enter the following:</p>
                          <p><strong><code>chrome --remote-debugging-port={debuggerPort} --user-data-dir=""{profilePath}"" {appRootUrl}</code></strong></p>";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return $@"<p>In a terminal window execute the following:</p>
                          <p><strong><code>google-chrome --remote-debugging-port={debuggerPort} --user-data-dir={profilePath} {appRootUrl}</code></strong></p>";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return $@"<p>Execute the following:</p>
                          <p><strong><code>open /Applications/Google\ Chrome.app --args --remote-debugging-port={debuggerPort} --user-data-dir={profilePath} {appRootUrl}</code></strong></p>";
            }
            else
            {
                throw new InvalidOperationException("Unknown OS platform");
            }
        }

        private static string GetLaunchEdgeInstructions(string appRootUrl)
        {
            var profilePath = Path.Combine(Path.GetTempPath(), "blazor-edge-debug");
            var debugggerPort = GetDebuggerPort();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $@"<p>Press Win+R and enter the following:</p>
                          <p><strong><code>msedge --remote-debugging-port={debugggerPort} --user-data-dir=""{profilePath}"" --no-first-run {appRootUrl}</code></strong></p>";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return $@"<p>In a terminal window execute the following:</p>
                          <p><strong><code>open /Applications/Microsoft\ Edge\ Dev.app --args --remote-debugging-port={debugggerPort} --user-data-dir={profilePath} {appRootUrl}</code></strong></p>";
            }
            else
            {
                throw new InvalidOperationException("Unknown OS platform");
            }
        }

        private static async Task<string> GetBrowserVersionInfoAsync()
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var debuggerHost = GetDebuggerHost();
            return await httpClient.GetStringAsync($"{debuggerHost}/json/version");
        }

        private static async Task<IEnumerable<BrowserTab>> GetOpenedBrowserTabs()
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var debuggerHost = GetDebuggerHost();
            var jsonResponse = await httpClient.GetStringAsync($"{debuggerHost}/json");
            return JsonSerializer.Deserialize<BrowserTab[]>(jsonResponse, JsonOptions);
        }

        class BrowserTab
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public string Url { get; set; }
            public string Title { get; set; }
            public string DevtoolsFrontendUrl { get; set; }
            public string WebSocketDebuggerUrl { get; set; }
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebAssembly.Net.Debugging;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides infrastructure for debugging Blazor WebAssembly applications.
    /// </summary>
    public static class WebAssemblyNetDebugProxyAppBuilderExtensions
    {
        private static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            IgnoreNullValues = true
        };

        private static string DefaultDebuggerHost = "http://localhost:9222";

        /// <summary>
        /// Adds middleware for needed for debugging Blazor WebAssembly applications
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
                    var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
                    return DebugWebSocketProxyRequest(loggerFactory, context);
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
                            var proxiedScheme = request.IsHttps ? "wss" : "ws";
                            var proxiedV8Endpoint = $"{proxiedScheme}://{request.Host}{request.PathBase}/_framework/debug/ws-proxy?browser={WebUtility.UrlEncode(underlyingV8Endpoint)}";
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
                        // VS Code's "js-debug" nightly extension, when configured to use the "pwa-chrome"
                        // debug type, uses the /json/version endpoint to find the websocket endpoint for
                        // debugging the browser that listens on a user-specified port.
                        //
                        // To make this flow work with the Mono debug proxy, we pass the request through
                        // to the underlying browser (to get its actual version info) but then overwrite
                        // the "webSocketDebuggerUrl" with the URL to the proxy.
                        //
                        // This whole connection flow isn't very good because it doesn't have any way
                        // to specify the debug port for the underlying browser. So, we end up assuming
                        // the default port 9222 in all cases. This is good enough for a manual "attach"
                        // but isn't good enough if the IDE is responsible for launching the browser,
                        // as it will be on a random port. So,
                        //
                        //  - VS isn't going to use this. Instead it will use a configured "debugEndpoint"
                        //    property from which it can construct the proxy URL directly (including adding
                        //    a "browser" querystring value to specify the underlying endpoint), bypassing
                        //    /json/version altogether
                        //  - We will need to update the VS Code debug adapter to make it do the same as VS
                        //    if there is a "debugEndpoint" property configured
                        //
                        // Once both VS and VS Code support the "debugEndpoint" flow, we should be able to
                        // remove this /json/version code altogether. We should check that in-browser
                        // debugging still works at that point.

                        var browserVersionJsonStream = await GetBrowserVersionInfoAsync();
                        var browserVersion = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(browserVersionJsonStream);

                        if (browserVersion.TryGetValue("webSocketDebuggerUrl", out var browserEndpoint))
                        {
                            var proxyEndpoint = GetProxyEndpoint(request, ((JsonElement)browserEndpoint).GetString());
                            browserVersion["webSocketDebuggerUrl"] = proxyEndpoint;
                        }

                        context.Response.ContentType = "application/json";
                        await JsonSerializer.SerializeAsync(context.Response.Body, browserVersion);
                    }
                }
                else
                {
                    await next();
                }
            });
        }

        private static async Task DebugWebSocketProxyRequest(ILoggerFactory loggerFactory, HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            var browserUri = new Uri(context.Request.Query["browser"]);
            var ideSocket = await context.WebSockets.AcceptWebSocketAsync();
            await new MonoProxy(loggerFactory).Run(browserUri, ideSocket);
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
            var proxyEndpoint = GetProxyEndpoint(request, underlyingV8Endpoint);
            var devToolsUrlAbsolute = new Uri(debuggerHost + tabToDebug.DevtoolsFrontendUrl);
            var devToolsUrlWithProxy = $"{devToolsUrlAbsolute.Scheme}://{devToolsUrlAbsolute.Authority}{devToolsUrlAbsolute.AbsolutePath}?{proxyEndpoint.Scheme}={proxyEndpoint.Authority}{proxyEndpoint.PathAndQuery}";
            context.Response.Redirect(devToolsUrlWithProxy);
        }

        private static Uri GetProxyEndpoint(HttpRequest incomingRequest, string browserEndpoint)
        {
            var builder = new UriBuilder(
                schemeName: incomingRequest.IsHttps ? "wss" : "ws",
                hostName: incomingRequest.Host.Host)
            {
                Path = $"{incomingRequest.PathBase}/_framework/debug/ws-proxy",
                Query = $"browser={WebUtility.UrlEncode(browserEndpoint)}"
            };

            if (incomingRequest.Host.Port.HasValue)
            {
                builder.Port = incomingRequest.Host.Port.Value;
            }

            return builder.Uri;
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
                return $@"<p>Edge is not current supported on your platform</p>";
            }
        }

        private static async Task<Stream> GetBrowserVersionInfoAsync()
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var debuggerHost = GetDebuggerHost();
            var response = await httpClient.GetAsync($"{debuggerHost}/json/version");
            return await response.Content.ReadAsStreamAsync();
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

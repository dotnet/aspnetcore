// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Components.WebAssembly.Server;

/// <summary>
/// Class for the target picker ui.
/// </summary>
public class TargetPickerUi
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _browserHost;
    private readonly string _debugProxyUrl;

    /// <summary>
    /// Initialize a new instance of <see cref="TargetPickerUi"/>.
    /// </summary>
    /// <param name="debugProxyUrl">The debug proxy url.</param>
    /// <param name="devToolsHost">The dev tools host.</param>
    public TargetPickerUi([StringSyntax(StringSyntaxAttribute.Uri)] string debugProxyUrl, string devToolsHost)
    {
        _debugProxyUrl = debugProxyUrl;
        _browserHost = devToolsHost;
    }

    /// <summary>
    /// Display the ui.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>The <see cref="Task"/>.</returns>
    public async Task DisplayFirefox(HttpContext context)
    {
        static async Task SendMessageToBrowser(NetworkStream toStream, JObject args, CancellationToken token)
        {
            var msg = args.ToString(Newtonsoft.Json.Formatting.None);
            var bytes = Encoding.UTF8.GetBytes(msg);
            byte[]? bytesWithHeader = Encoding.UTF8.GetBytes($"{bytes.Length}:").Concat(bytes).ToArray();
            await toStream.WriteAsync(bytesWithHeader, token).AsTask();
        }
#pragma warning disable CA1835
        static async Task<string> ReceiveMessageLoop(TcpClient browserDebugClientConnect, CancellationToken token)
        {
            NetworkStream toStream = browserDebugClientConnect.GetStream();
            int bytesRead = 0;
            var _lengthBuffer = new byte[10];
            while (bytesRead == 0 || Convert.ToChar(_lengthBuffer[bytesRead - 1]) != ':')
            {
                if (!browserDebugClientConnect.Connected)
                {
                    return "";
                }

                if (bytesRead + 1 > _lengthBuffer.Length)
                {
                    throw new IOException($"Protocol error: did not get the expected length preceding a message, " +
                                          $"after reading {bytesRead} bytes. Instead got: {Encoding.UTF8.GetString(_lengthBuffer)}");
                }

                int readLen = await toStream.ReadAsync(_lengthBuffer, bytesRead, 1, token);
                bytesRead += readLen;
            }
            string str = Encoding.UTF8.GetString(_lengthBuffer, 0, bytesRead - 1);
            if (!int.TryParse(str, out int messageLen))
            {
                return "";
            }
            byte[] buffer = new byte[messageLen];
            bytesRead = await toStream.ReadAsync(buffer, 0, messageLen, token);
            while (bytesRead != messageLen)
            {
                if (!browserDebugClientConnect.Connected)
                {
                    return "";
                }
                bytesRead += await toStream.ReadAsync(buffer, bytesRead, messageLen - bytesRead, token);
            }
            var messageReceived = Encoding.UTF8.GetString(buffer, 0, messageLen);
            return messageReceived;
        }
        static async Task EvaluateOnBrowser(NetworkStream toStream, string? to, string text, CancellationToken token)
        {
            //System.Diagnostics.Debugger.Launch();
            var o = JObject.FromObject(new
            {
                to,
                type = "evaluateJSAsync",
                text,
                options = new { eager = true, mapped = new { @await = true } }
            });
            await SendMessageToBrowser(toStream, o, token);
        }
#pragma warning restore CA1835

        context.Response.ContentType = "text/html";
        var request = context.Request;
        var targetApplicationUrl = request.Query["url"];
        var browserDebugClientConnect = new TcpClient();
        if (IPEndPoint.TryParse(_debugProxyUrl, out IPEndPoint? endpoint))
        {
            try
            {
                await browserDebugClientConnect.ConnectAsync(endpoint.Address, 6000);
            }
            catch (Exception)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($@"
                                    Open firefox with remote debugging enabled on port 6000<br>
                                    <tt>firefox --start-debugger-server 6000 -new-tab about:debugging</tt>");
                return;
            }
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            NetworkStream toStream = browserDebugClientConnect.GetStream();
            await SendMessageToBrowser(toStream, JObject.FromObject(new { type = "listTabs", to = "root" }), token);
            string ?tabToRedirect = null;
            bool foundAboutDebugging = false;
            string? consoleActorId = null;
            string ?toCmd = null;
            while (browserDebugClientConnect.Connected)
            {
                var res = JObject.Parse(await ReceiveMessageLoop(browserDebugClientConnect, token));
                var tabs = res["tabs"]?.Value<JArray>();
                if (res?["type"]?.Value<string>()?.Equals("tabListChanged") == true)
                {
                    await SendMessageToBrowser(toStream, JObject.FromObject(new { type = "listTabs", to = "root" }), token);
                }
                else if (tabs != null)
                {
                    foreach (var tab in tabs)
                    {
                        var urlInTab = tab["url"]?.Value<string>();
                        if (string.IsNullOrEmpty(consoleActorId))
                        {
                            if (urlInTab?.StartsWith("about:debugging#", StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                foundAboutDebugging = true;
                                toCmd = tab["actor"]?.Value<string>();
                                if (!string.IsNullOrEmpty(tabToRedirect))
                                {
                                    break;
                                }
                            }
                            if (urlInTab?.Equals(targetApplicationUrl) == true)
                            {
                                tabToRedirect = tab["browserId"]?.Value<string>();
                                if (foundAboutDebugging)
                                {
                                    break;
                                }
                            }
                        }
                        else if (urlInTab?.StartsWith("about:devtools", StringComparison.InvariantCultureIgnoreCase) == true)
                        {
                            return;
                        }
                    }
                    if (!foundAboutDebugging)
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("WARNING: Open about:debugging tab before pressing Debugging Hotkey");
                        return;
                    }
                    if (string.IsNullOrEmpty(consoleActorId))
                    {
                        await EvaluateOnBrowser(toStream, consoleActorId, $"if (AboutDebugging.store.getState().runtimes.networkRuntimes.find(element => element.id == \"{_debugProxyUrl}\").runtimeDetails !== null) {{ AboutDebugging.actions.selectPage(\"runtime\", \"{_debugProxyUrl}\"); if (AboutDebugging.store.getState().runtimes.selectedRuntimeId == \"{_debugProxyUrl}\") AboutDebugging.actions.inspectDebugTarget(\"tab\", {tabToRedirect})}};", token);
                    }
                }
                if (!string.IsNullOrEmpty(consoleActorId))
                {
                    if (res?["input"]?.Value<string>()?.StartsWith("AboutDebugging.actions.addNetworkLocation(", StringComparison.InvariantCultureIgnoreCase) == true)
                    {
                        await EvaluateOnBrowser(toStream, consoleActorId, $"if (AboutDebugging.store.getState().runtimes.networkRuntimes.find(element => element.id == \"{_debugProxyUrl}\").runtimeDetails !== null) {{ AboutDebugging.actions.selectPage(\"runtime\", \"{_debugProxyUrl}\"); if (AboutDebugging.store.getState().runtimes.selectedRuntimeId == \"{_debugProxyUrl}\") AboutDebugging.actions.inspectDebugTarget(\"tab\", {tabToRedirect})}};", token);
                    }
                    if (res?["input"]?.Value<string>()?.StartsWith("if (AboutDebugging.store.getState()", StringComparison.InvariantCultureIgnoreCase) == true)
                    {
                        await EvaluateOnBrowser(toStream, consoleActorId, $"if (AboutDebugging.store.getState().runtimes.networkRuntimes.find(element => element.id == \"{_debugProxyUrl}\").runtimeDetails !== null) {{ AboutDebugging.actions.selectPage(\"runtime\", \"{_debugProxyUrl}\"); if (AboutDebugging.store.getState().runtimes.selectedRuntimeId == \"{_debugProxyUrl}\") AboutDebugging.actions.inspectDebugTarget(\"tab\", {tabToRedirect})}};", token);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(res?["target"]?["consoleActor"]?.Value<string>()))
                    {
                        consoleActorId = res["target"]?["consoleActor"]?.Value<string>();
                        await EvaluateOnBrowser(toStream, consoleActorId, $"AboutDebugging.actions.addNetworkLocation(\"{_debugProxyUrl}\"); AboutDebugging.actions.connectRuntime(\"{_debugProxyUrl}\");", token);
                    }
                    else if (!string.IsNullOrEmpty(res?["actor"]?.Value<string>()))
                    {
                        await SendMessageToBrowser(toStream, JObject.FromObject(new { type = "watchTargets", targetType = "frame", to = res?["actor"]?.Value<string>() }), token);
                        await SendMessageToBrowser(toStream, JObject.FromObject(new { type = "watchResources", resourceTypes = new JArray("console-message"), to = res?["actor"]?.Value<string>() }), token);
                    }
                    else if (!string.IsNullOrEmpty(toCmd))
                    {
                        await SendMessageToBrowser(toStream, JObject.FromObject(new { type = "getWatcher", isServerTargetSwitchingEnabled = true, to = toCmd }), token);
                    }
                }
            }

        }
        return;
    }

    /// <summary>
    /// Display the ui.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>The <see cref="Task"/>.</returns>
    public async Task Display(HttpContext context)
    {
        context.Response.ContentType = "text/html";

        var request = context.Request;
        var targetApplicationUrl = request.Query["url"];

        var debuggerTabsListUrl = $"{_browserHost}/json";
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
    <h4>If you are using Google Chrome or Chromium for your development, follow these instructions:</h4>
    {GetLaunchChromeInstructions(targetApplicationUrl.ToString())}
</p>
<p>
    <h4>If you are using Microsoft Edge (80+) for your development, follow these instructions:</h4>
    {GetLaunchEdgeInstructions(targetApplicationUrl.ToString())}
</p>
<strong>This should launch a new browser window with debugging enabled..</p>
<h2>Underlying exception:</h2>
<pre>{ex}</pre>
                ");

            return;
        }

        var matchingTabs = string.IsNullOrEmpty(targetApplicationUrl)
            ? availableTabs.ToList()
            : availableTabs.Where(t => t.Url.Equals(targetApplicationUrl, StringComparison.Ordinal)).ToList();

        if (matchingTabs.Count == 1)
        {
            // We know uniquely which tab to debug, so just redirect
            var devToolsUrlWithProxy = GetDevToolsUrlWithProxy(matchingTabs.Single());
            context.Response.Redirect(devToolsUrlWithProxy);
        }
        else if (matchingTabs.Count == 0)
        {
            await context.Response.WriteAsync("<h1>No inspectable pages found</h1>");

            var suffix = string.IsNullOrEmpty(targetApplicationUrl)
                ? string.Empty
                : $" matching the URL {WebUtility.HtmlEncode(targetApplicationUrl)}";
            await context.Response.WriteAsync($"<p>The list of targets returned by {WebUtility.HtmlEncode(debuggerTabsListUrl)} contains no entries{suffix}.</p>");
            await context.Response.WriteAsync("<p>Make sure your browser is displaying the target application.</p>");
        }
        else
        {
            await context.Response.WriteAsync("<h1>Inspectable pages</h1>");
            await context.Response.WriteAsync(@"
                    <style type='text/css'>
                        body {
                            font-family: Helvetica, Arial, sans-serif;
                            margin: 2rem 3rem;
                        }

                        .inspectable-page {
                            display: block;
                            background-color: #eee;
                            padding: 1rem 1.2rem;
                            margin-bottom: 1rem;
                            border-radius: 0.5rem;
                            text-decoration: none;
                            color: #888;
                        }

                        .inspectable-page:hover {
                            background-color: #fed;
                        }

                        .inspectable-page h3 {
                            margin-top: 0px;
                            margin-bottom: 0.3rem;
                            color: black;
                        }
                    </style>
                ");

            foreach (var tab in matchingTabs)
            {
                var devToolsUrlWithProxy = GetDevToolsUrlWithProxy(tab);
                await context.Response.WriteAsync(
                    $"<a class='inspectable-page' href='{WebUtility.HtmlEncode(devToolsUrlWithProxy)}'>"
                    + $"<h3>{WebUtility.HtmlEncode(tab.Title)}</h3>{WebUtility.HtmlEncode(tab.Url)}"
                    + $"</a>");
            }
        }
    }

    private string GetDevToolsUrlWithProxy(BrowserTab tabToDebug)
    {
        var underlyingV8Endpoint = new Uri(tabToDebug.WebSocketDebuggerUrl);
        var proxyEndpoint = new Uri(_debugProxyUrl);
        var devToolsUrlAbsolute = new Uri(_browserHost + tabToDebug.DevtoolsFrontendUrl);
        var devToolsUrlWithProxy = $"{devToolsUrlAbsolute.Scheme}://{devToolsUrlAbsolute.Authority}{devToolsUrlAbsolute.AbsolutePath}?{underlyingV8Endpoint.Scheme}={proxyEndpoint.Authority}{underlyingV8Endpoint.PathAndQuery}";
        return devToolsUrlWithProxy;
    }

    private string GetLaunchChromeInstructions(string targetApplicationUrl)
    {
        var profilePath = Path.Combine(Path.GetTempPath(), "blazor-chrome-debug");
        var debuggerPort = new Uri(_browserHost).Port;

        if (OperatingSystem.IsWindows())
        {
            return $@"<p>Press Win+R and enter the following:</p>
                          <p><strong><code>chrome --remote-debugging-port={debuggerPort} --user-data-dir=""{profilePath}"" {targetApplicationUrl}</code></strong></p>";
        }
        else if (OperatingSystem.IsLinux())
        {
            return $@"<p>In a terminal window execute the following:</p>
                          <p><strong><code>google-chrome --remote-debugging-port={debuggerPort} --user-data-dir={profilePath} {targetApplicationUrl}</code></strong></p>";
        }
        else if (OperatingSystem.IsMacOS())
        {
            return $@"<p>Execute the following:</p>
                          <p><strong><code>open -n /Applications/Google\ Chrome.app --args --remote-debugging-port={debuggerPort} --user-data-dir={profilePath} {targetApplicationUrl}</code></strong></p>";
        }
        else
        {
            throw new InvalidOperationException("Unknown OS platform");
        }
    }

    private string GetLaunchEdgeInstructions(string targetApplicationUrl)
    {
        var profilePath = Path.Combine(Path.GetTempPath(), "blazor-edge-debug");
        var debuggerPort = new Uri(_browserHost).Port;

        if (OperatingSystem.IsWindows())
        {
            return $@"<p>Press Win+R and enter the following:</p>
                          <p><strong><code>msedge --remote-debugging-port={debuggerPort} --user-data-dir=""{profilePath}"" --no-first-run {targetApplicationUrl}</code></strong></p>";
        }
        else if (OperatingSystem.IsMacOS())
        {
            return $@"<p>In a terminal window execute the following:</p>
                          <p><strong><code>open -n /Applications/Microsoft\ Edge.app --args --remote-debugging-port={debuggerPort} --user-data-dir={profilePath} {targetApplicationUrl}</code></strong></p>";
        }
        else
        {
            return $@"<p>Edge is not current supported on your platform</p>";
        }
    }

    private async Task<IEnumerable<BrowserTab>> GetOpenedBrowserTabs()
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var jsonResponse = await httpClient.GetStringAsync($"{_browserHost}/json");
        return JsonSerializer.Deserialize<BrowserTab[]>(jsonResponse, JsonOptions)!;
    }

    private sealed record BrowserTab
    (
        string Id,
        string Type,
        string Url,
        string Title,
        string DevtoolsFrontendUrl,
        string WebSocketDebuggerUrl
    );
}
